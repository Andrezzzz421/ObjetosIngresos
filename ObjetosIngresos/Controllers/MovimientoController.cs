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
            var elementos = await _srvMovimiento.BuscarElementosAsync(query ?? "");

            var resultado = new List<object>();
            foreach (var e in elementos)
            {
                var movActivo = await _srvMovimiento.GetMovimientoActivoAsync(e.IdElemento);
                var fotoBase64 = e.FotoArchivo != null && e.FotoArchivo.Length > 0
                    ? APHelpers.ToBase64(e.FotoArchivo)
                    : null;

                resultado.Add(new
                {
                    idElemento = e.IdElemento,
                    tipoElemento = e.TipoElemento,
                    marca = e.IdMarcaNavigation?.NombreMarca ?? "Sin marca",
                    serial = e.Serial ?? "N/A",
                    propietario = e.IdUsuarioNavigation != null
                        ? $"{e.IdUsuarioNavigation.Nombres} {e.IdUsuarioNavigation.Apellidos}"
                        : "Desconocido",
                    documento = e.IdUsuarioNavigation?.Documento ?? "N/A",
                    foto = fotoBase64,
                    tieneMovimientoActivo = movActivo != null,
                    idMovimientoActivo = movActivo?.IdMovimiento,
                    fechaEntrada = movActivo?.FechaEntrada?.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt"),
                    sedeEntrada = movActivo?.IdSedeNavigation?.NombreSede
                });
            }

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
    }
}
