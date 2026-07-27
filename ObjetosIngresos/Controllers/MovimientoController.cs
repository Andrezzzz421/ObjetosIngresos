using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;

namespace ObjetosIngresos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MovimientoController : Controller
    {
        private readonly MovimientoServices _srvMovimiento;
        private readonly SistemaIngresoContext _db;

        public MovimientoController(MovimientoServices srvMovimiento, SistemaIngresoContext db)
        {
            _srvMovimiento = srvMovimiento;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Sedes = await _db.Sedes.OrderBy(s => s.NombreSede).ToListAsync();

            // Dashboard stats
            ViewBag.TotalEquipos = await _db.Elementos.CountAsync();

            var colombiaNow = DateTime.UtcNow.AddHours(-5);
            var todayColombiaStartUtc = colombiaNow.Date.AddHours(5);
            var tomorrowColombiaStartUtc = todayColombiaStartUtc.AddDays(1);

            ViewBag.EquiposIngresadosHoy = await _db.RegistrosMovimientos
                .Where(m => m.FechaEntrada >= todayColombiaStartUtc && m.FechaEntrada < tomorrowColombiaStartUtc)
                .CountAsync();

            ViewBag.EquiposDentro = await _db.RegistrosMovimientos
                .Where(m => m.FechaSalida == null)
                .CountAsync();

            return View("~/Views/Movimiento/Index.cshtml");
        }
        [HttpGet]
        public async Task<IActionResult> Buscar(string query)
        {
            var q = query?.Trim().ToLower() ?? "";

            // 1. Preparamos la consulta sobre Elementos sin Include() masivos
            IQueryable<Elemento> queryBase = _db.Elementos.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                queryBase = queryBase.Where(e =>
                    (e.IdUsuarioNavigation != null && e.IdUsuarioNavigation.Documento != null && e.IdUsuarioNavigation.Documento.ToLower().Contains(q)) ||
                    (e.Serial != null && e.Serial.ToLower().Contains(q)));
            }
            else
            {
                // Limitar a los últimos 30 en lugar de 100 para la carga inicial
                queryBase = queryBase.OrderByDescending(e => e.IdElemento).Take(30);
            }

            // 2. Proyección directa a SQL: La BD solo devolverá los campos estrictamente necesarios
            var elementosProyectados = await queryBase.Select(e => new
            {
                idElemento = e.IdElemento,
                tipoElemento = e.TipoElemento,
                marca = e.IdMarcaNavigation != null ? e.IdMarcaNavigation.NombreMarca : "Sin marca",
                serial = e.Serial ?? "N/A",
                propietario = e.IdUsuarioNavigation != null
                    ? e.IdUsuarioNavigation.Nombres + " " + e.IdUsuarioNavigation.Apellidos
                    : "Desconocido",
                documento = e.IdUsuarioNavigation != null ? e.IdUsuarioNavigation.Documento : "N/A",

                // Verificamos si tiene foto sin traer los bytes completos
                tieneFoto = e.FotoArchivo != null,

                // Traemos SOLO el movimiento activo (el que no tiene fecha de salida)
                movActivo = e.RegistrosMovimientos
                    .Where(m => m.FechaSalida == null)
                    .OrderByDescending(m => m.FechaEntrada)
                    .Select(m => new
                    {
                        m.IdMovimiento,
                        m.FechaEntrada,
                        NombreSede = m.IdSedeNavigation != null ? m.IdSedeNavigation.NombreSede : "N/A"
                    })
                    .FirstOrDefault()
            }).ToListAsync();

            // 3. Mapeo final rápido en memoria
            var resultado = elementosProyectados.Select(e => new
            {
                e.idElemento,
                e.tipoElemento,
                e.marca,
                e.serial,
                e.propietario,
                e.documento,
                foto = e.tieneFoto ? $"/Movimiento/GetFoto?id={e.idElemento}" : null,
                tieneMovimientoActivo = e.movActivo != null,
                idMovimientoActivo = e.movActivo?.IdMovimiento,
                fechaEntrada = e.movActivo?.FechaEntrada != null
                    ? e.movActivo.FechaEntrada.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt")
                    : null,
                sedeEntrada = e.movActivo?.NombreSede
            });

            return Json(new { success = true, data = resultado });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn([FromForm] int idElemento, [FromForm] int idSede)
        {
            try
            {
                var movimiento = await _srvMovimiento.RegistrarEntradaAsync(idElemento, idSede);
                string fechaFormateada = null;

                if (movimiento.FechaEntrada.HasValue)
                {
                    fechaFormateada = movimiento.FechaEntrada.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt");
                }

                return Json(new
                {
                    success = true,
                    message = "✅ Entrada registrada correctamente.",
                    idMovimiento = movimiento.IdMovimiento,
                    fechaEntrada = fechaFormateada
                });
            }

            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error al registrar la entrada. Intente de nuevo." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut([FromForm] int idMovimiento)
        {
            try
            {
                var movimiento = await _srvMovimiento.RegistrarSalidaAsync(idMovimiento);
                return Json(new
                {
                    success = true,
                    message = "✅ Salida registrada correctamente.",
                    fechaSalida = movimiento.FechaSalida?.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt")
                });
            }
            catch (KeyNotFoundException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al registrar la salida: " + ex.ToString() });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /Movimiento/Historial?idElemento=x  — Historial de movimientos
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Historial(int idElemento)
        {
            var historial = await _srvMovimiento.GetHistorialAsync(idElemento);

            var resultado = historial.Select(m => new
            {
                idMovimiento = m.IdMovimiento,
                fechaEntrada = m.FechaEntrada?.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt"),
                fechaSalida = m.FechaSalida?.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt") ?? "—",
                sede = m.IdSedeNavigation?.NombreSede ?? "N/A",
                estado = m.FechaSalida == null ? "Activo" : "Finalizado"
            });

            return Json(new { success = true, data = resultado });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /Movimiento/GetFoto?id=x  — Devuelve la imagen como archivo
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetFoto(int id)
        {
            var fotoBytes = await _db.Elementos
                .Where(e => e.IdElemento == id)
                .Select(e => e.FotoArchivo)
                .FirstOrDefaultAsync();

            if (fotoBytes == null || fotoBytes.Length == 0)
            {
                return NotFound();
            }

            return File(fotoBytes, "image/jpeg");
        }
    }
}
