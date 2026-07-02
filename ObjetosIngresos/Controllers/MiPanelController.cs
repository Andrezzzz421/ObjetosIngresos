using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    [Authorize]
    public class MiPanelController : Controller
    {
        private readonly SistemaIngresoContext _db;

        public MiPanelController(SistemaIngresoContext db)
        {
            _db = db;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers de sesión
        // ─────────────────────────────────────────────────────────────────────

        private async Task<Usuario?> GetUsuarioSesionAsync()
        {
            var doc = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(doc)) return null;

            return await _db.Usuarios
                .Include(u => u.Elementos)
                    .ThenInclude(e => e.IdMarcaNavigation)
                .Include(u => u.Elementos)
                    .ThenInclude(e => e.RegistrosMovimientos)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == doc.Trim());
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /MiPanel  — Dashboard principal del usuario
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var usuario = await GetUsuarioSesionAsync();
            if (usuario == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            // Si no tiene objetos registrados → ir directo al formulario
            if (!usuario.Elementos.Any())
                return RedirectToAction(nameof(RegistrarEquipo));

            return View("~/Views/MiPanel/Index.cshtml", usuario);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /MiPanel/RegistrarEquipo
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> RegistrarEquipo()
        {
            var usuario = await GetUsuarioSesionAsync();
            if (usuario == null) return RedirectToAction("Login", "Auth");

            ViewBag.Marcas = new SelectList(await _db.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca");
            return View("~/Views/MiPanel/RegistrarEquipo.cshtml");
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /MiPanel/RegistrarEquipo
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEquipo(string tipoElemento, int? idMarca, string? serial, IFormFile? foto)
        {
            var usuario = await GetUsuarioSesionAsync();
            if (usuario == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(tipoElemento))
            {
                ModelState.AddModelError("tipoElemento", "El tipo de elemento es obligatorio.");
                ViewBag.Marcas = new SelectList(await _db.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca");
                return View("~/Views/MiPanel/RegistrarEquipo.cshtml");
            }

            var elemento = new Elemento
            {
                IdUsuario = usuario.IdUsuario,
                TipoElemento = tipoElemento.Trim(),
                IdMarca = idMarca,
                Serial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim()
            };

            if (foto != null && foto.Length > 0)
                elemento.FotoArchivo = APHelpers.ToBytes(foto);

            _db.Elementos.Add(elemento);
            await _db.SaveChangesAsync();

            TempData["Exito"] = "¡Equipo registrado correctamente!";
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /MiPanel/EliminarEquipo
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEquipo(int id)
        {
            var usuario = await GetUsuarioSesionAsync();
            if (usuario == null)
                return Json(new { success = false, message = "Sesión no válida." });

            var elemento = await _db.Elementos
                .Include(e => e.DetalleElementos)
                .FirstOrDefaultAsync(e => e.IdElemento == id && e.IdUsuario == usuario.IdUsuario);

            if (elemento == null)
                return Json(new { success = false, message = "Equipo no encontrado o no te pertenece." });

            // Verificar si tiene movimientos activos (sin salida)
            var tieneMovimientoActivo = await _db.RegistrosMovimientos
                .AnyAsync(m => m.IdElemento == id && m.FechaSalida == null);

            if (tieneMovimientoActivo)
                return Json(new { success = false, message = "No puedes eliminar un equipo que tiene un ingreso activo. Primero registra su salida." });

            // Eliminar detalles y el elemento
            _db.DetalleElementos.RemoveRange(elemento.DetalleElementos);
            _db.Elementos.Remove(elemento);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
