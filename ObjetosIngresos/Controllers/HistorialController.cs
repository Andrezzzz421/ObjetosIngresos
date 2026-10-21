using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ObjetosIngresos.Controllers
{
    [Authorize]
    public class HistorialController : Controller
    {
        private readonly IHistorialService _historialService;
        private readonly SistemaIngresoContext _db;

        public HistorialController(IHistorialService historialService, SistemaIngresoContext db)
        {
            _historialService = historialService;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] HistorialFiltroDto filtro)
        {
            filtro ??= new HistorialFiltroDto();
            var registros = await _historialService.ObtenerMovimientosHistorialAsync(filtro);

            // Estadísticas rápidas para los indicadores visuales
            ViewBag.TotalEventos = registros.Count;
            ViewBag.TotalEntradas = registros.Count;
            ViewBag.TotalSalidasEquipo = registros.Count(r => r.FechaSalida.HasValue);
            ViewBag.TotalSalidasPersona = registros.Count(r => !r.FechaSalida.HasValue);
            ViewBag.TotalNovedades = registros.Count(r => r.TieneNovedad);
            ViewBag.Filtro = filtro;

            return View(registros);
        }

        [HttpGet]
        public async Task<IActionResult> Buscar([FromQuery] HistorialFiltroDto filtro)
        {
            filtro ??= new HistorialFiltroDto();
            var registros = await _historialService.ObtenerMovimientosHistorialAsync(filtro);

            var resultado = registros.Select(r => new
            {
                id = r.IdMovimiento,
                idMovimiento = r.IdMovimiento,
                fechaEntrada = r.FechaEntradaTexto,
                fechaSalida = r.FechaSalidaTexto,
                estado = r.Estado,
                persona = r.Persona,
                documento = r.Documento,
                ficha = r.Ficha ?? "N/A",
                rol = r.Rol,
                esIndependiente = r.EsIndependiente,
                condicionTexto = r.CondicionTexto,
                equipo = r.Equipo,
                marca = r.Marca,
                serial = r.Serial,
                sede = r.Sede,
                observaciones = r.Observaciones ?? "Sin observaciones",
                accesorios = r.Accesorios,
                tieneNovedad = r.TieneNovedad
            });

            return Json(new
            {
                success = true,
                total = registros.Count,
                stats = new
                {
                    total = registros.Count,
                    entradas = registros.Count,
                    salidasEquipo = registros.Count(r => r.FechaSalida.HasValue),
                    salidasPersona = registros.Count(r => !r.FechaSalida.HasValue),
                    novedades = registros.Count(r => r.TieneNovedad)
                },
                data = resultado
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportarCsv([FromQuery] HistorialFiltroDto filtro)
        {
            filtro ??= new HistorialFiltroDto();
            filtro.Take = 5000; // Permitir hasta 5000 registros para la exportación

            var bytes = await _historialService.ExportarCsvAsync(filtro);
            var nombreArchivo = $"Historial_Auditoria_{DateTime.UtcNow.AddHours(-5):yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
        }

        [HttpGet]
        public async Task<IActionResult> Trazabilidad(int idMovimiento)
        {
            if (idMovimiento <= 0)
            {
                return Json(new { success = false, message = "ID de movimiento inválido." });
            }

            var timeline = await _historialService.ObtenerTrazabilidadMovimientoAsync(idMovimiento);

            // Obtener metadatos descriptivos del equipo y propietario para la cabecera del modal
            var mov = await _db.RegistrosMovimientos
                .Include(m => m.IdElementoNavigation)
                    .ThenInclude(e => e.IdMarcaNavigation)
                .Include(m => m.IdElementoNavigation)
                    .ThenInclude(e => e.IdUsuarioNavigation)
                .Include(m => m.IdSedeNavigation)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento);

            var infoCabecera = new
            {
                idMovimiento,
                equipo = mov?.IdElementoNavigation?.TipoElemento ?? "Equipo",
                marca = mov?.IdElementoNavigation?.IdMarcaNavigation?.NombreMarca ?? "N/A",
                serial = mov?.IdElementoNavigation?.Serial ?? "N/A",
                propietario = mov?.IdElementoNavigation?.IdUsuarioNavigation != null
                    ? $"{mov.IdElementoNavigation.IdUsuarioNavigation.Nombres} {mov.IdElementoNavigation.IdUsuarioNavigation.Apellidos}".Trim()
                    : "Desconocido",
                documento = mov?.IdElementoNavigation?.IdUsuarioNavigation?.Documento ?? "N/A",
                sede = mov?.IdSedeNavigation?.NombreSede ?? "N/A"
            };

            return Json(new
            {
                success = true,
                movimiento = infoCabecera,
                timeline
            });
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "ID de evento inválido." });

            var r = await _db.HistorialSistema
                .Include(h => h.Persona)
                .Include(h => h.Equipo)
                    .ThenInclude(e => e.IdMarcaNavigation)
                .Include(h => h.UsuarioRegistra)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.Id == id);

            if (r == null) return NotFound(new { success = false, message = "Evento no encontrado." });

            return Json(new
            {
                success = true,
                data = new
                {
                    id = r.Id,
                    tipoEvento = r.TipoEvento,
                    idMovimiento = r.IdMovimiento,
                    fechaEvento = r.FechaEvento.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt"),
                    fechaRegistro = r.FechaRegistro.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt"),
                    descripcion = r.Descripcion,
                    persona = r.Persona != null ? $"{r.Persona.Nombres} {r.Persona.Apellidos}".Trim() : "Desconocido",
                    documento = r.Persona?.Documento ?? "N/A",
                    equipo = r.Equipo?.TipoElemento ?? "Equipo",
                    marca = r.Equipo?.IdMarcaNavigation?.NombreMarca ?? "Sin marca",
                    serial = r.Equipo?.Serial ?? "N/A",
                    usuarioRegistra = r.UsuarioRegistra != null ? $"{r.UsuarioRegistra.Nombres} {r.UsuarioRegistra.Apellidos}".Trim() : "Sistema",
                    datosNuevos = r.DatosNuevos,
                    datosAnteriores = r.DatosAnteriores
                }
            });
        }
    }
}
