using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using ObjetosIngresos.Models.ViewModels;

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

        #region Métodos Privados Auxiliares

        private async Task<int?> ObtenerIdUsuarioSesionAsync()
        {
            var doc = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrWhiteSpace(doc)) return null;

            return await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Documento == doc.Trim())
                .Select(u => (int?)u.IdUsuario)
                .FirstOrDefaultAsync();
        }

        private async Task CargarViewBagsAsync()
        {
            ViewBag.Marcas = new SelectList(
                await _db.Marcas.AsNoTracking().OrderBy(m => m.NombreMarca).ToListAsync(),
                "IdMarca",
                "NombreMarca"
            );

            ViewBag.TiposDetalle = await _db.TiposDetalles
                .AsNoTracking()
                .OrderBy(t => t.Nombre)
                .ToListAsync();
        }

        #endregion

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            var doc = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(doc))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            var limpio = doc.Trim();
            var usuarioInfo = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Documento == limpio)
                .Select(u => new
                {
                    u.Nombres,
                    Elementos = u.Elementos.Select(e => new MiPanelEquipoViewModel
                    {
                        IdElemento = e.IdElemento,
                        TipoElemento = e.TipoElemento,
                        Serial = e.Serial,
                        NombreMarca = e.IdMarcaNavigation != null ? e.IdMarcaNavigation.NombreMarca : "Sin marca",
                        TieneFoto = e.FotoArchivo != null,
                        TieneIngresoActivo = e.RegistrosMovimientos.Any(m => m.FechaSalida == null)
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (usuarioInfo == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            if (!usuarioInfo.Elementos.Any())
            {
                return RedirectToAction(nameof(RegistrarEquipo));
            }

            var model = new MiPanelViewModel
            {
                Nombres = usuarioInfo.Nombres,
                Elementos = usuarioInfo.Elementos
            };

            return View("~/Views/MiPanel/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> RegistrarEquipo()
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null) return RedirectToAction("Login", "Auth");

            await CargarViewBagsAsync();
            return View("~/Views/MiPanel/RegistrarEquipo.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEquipo(List<RegistrarEquipoViewModel> Equipos, List<IFormFile> FotosEquipos)
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null) return RedirectToAction("Login", "Auth");

            if (Equipos == null || !Equipos.Any())
            {
                ModelState.AddModelError("", "Debes agregar al menos un equipo a la lista.");
                await CargarViewBagsAsync();
                return View("~/Views/MiPanel/RegistrarEquipo.cshtml");
            }

            // Obtener la estrategia de ejecución para PostgreSQL con Retry
            var strategy = _db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync();

                    for (int i = 0; i < Equipos.Count; i++)
                    {
                        var item = Equipos[i];

                        var elemento = new Elemento
                        {
                            IdUsuario = idUsuario.Value,
                            TipoElemento = item.TipoElemento?.Trim(),
                            IdMarca = item.IdMarca > 0 ? item.IdMarca : null,
                            Serial = string.IsNullOrWhiteSpace(item.NumeroSerie) ? null : item.NumeroSerie.Trim()
                        };

                        // Validar la foto correspondiente al índice i
                        if (FotosEquipos != null && i < FotosEquipos.Count)
                        {
                            var foto = FotosEquipos[i];
                            if (foto != null && foto.Length > 0 && foto.ContentType.StartsWith("image/"))
                            {
                                elemento.FotoArchivo = await APHelpers.ToBytes(foto);
                            }
                        }

                        _db.Elementos.Add(elemento);
                        await _db.SaveChangesAsync(); // Genera el IdElemento

                        // Registrar los accesorios válidos
                        if (item.Detalles != null && item.Detalles.Any())
                        {
                            var detallesValidos = item.Detalles.Where(d => d.IdTipoDetalle > 0).ToList();

                            foreach (var det in detallesValidos)
                            {
                                var detalleElemento = new DetalleElemento
                                {
                                    IdDetalle = 0,
                                    IdElemento = elemento.IdElemento,
                                    IdTipoDetalle = det.IdTipoDetalle
                                };
                                _db.DetalleElementos.Add(detalleElemento);
                            }

                            if (detallesValidos.Any())
                            {
                                await _db.SaveChangesAsync();
                            }
                        }
                    }

                    await transaction.CommitAsync();
                });

                TempData["Exito"] = "¡Todos los equipos fueron registrados correctamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                var mensajeError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModelState.AddModelError("", "Ocurrió un error al intentar guardar: " + mensajeError);

                await CargarViewBagsAsync();
                return View("~/Views/MiPanel/RegistrarEquipo.cshtml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEquipo(int id)
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null)
                return Json(new { success = false, message = "Sesión no válida." });

            var elemento = await _db.Elementos
                .Include(e => e.DetalleElementos)
                .FirstOrDefaultAsync(e => e.IdElemento == id && e.IdUsuario == idUsuario.Value);

            if (elemento == null)
                return Json(new { success = false, message = "Equipo no encontrado o no te pertenece." });

            var tieneMovimientoActivo = await _db.RegistrosMovimientos
                .AnyAsync(m => m.IdElemento == id && m.FechaSalida == null);

            if (tieneMovimientoActivo)
                return Json(new { success = false, message = "No puedes eliminar un equipo que tiene un ingreso activo. Primero registra su salida." });

            // Eliminación en cascada de movimientos y sus detalles
            var movimientos = await _db.RegistrosMovimientos.Where(m => m.IdElemento == id).ToListAsync();
            if (movimientos.Any())
            {
                var idMovimientos = movimientos.Select(m => m.IdMovimiento).ToList();
                var detallesMov = await _db.MovimientoDetalles.Where(md => idMovimientos.Contains(md.IdMovimiento)).ToListAsync();

                if (detallesMov.Any())
                {
                    _db.MovimientoDetalles.RemoveRange(detallesMov);
                }
                _db.RegistrosMovimientos.RemoveRange(movimientos);
            }

            _db.DetalleElementos.RemoveRange(elemento.DetalleElementos);
            _db.Elementos.Remove(elemento);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}