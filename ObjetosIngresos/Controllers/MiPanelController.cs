using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using ObjetosIngresos.ViewModel;


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

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            if (User.IsInRole("Administrador"))
            {
                context.Result = RedirectToAction("Index", "Admin");
            }
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
                        TieneIngresoActivo = e.RegistrosMovimientos.Any(m => m.FechaSalida == null),
                        ActivoParaIngreso = e.ActivoParaIngreso,
                        Accesorios = e.DetalleElementos
                            .Where(d => d.IdTipoDetalleNavigation != null)
                            .Select(d => d.IdTipoDetalleNavigation.Nombre)
                            .ToList()
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

                        if (FotosEquipos != null && i < FotosEquipos.Count)
                        {
                            var foto = FotosEquipos[i];
                            if (foto != null && foto.Length > 0 && foto.ContentType.StartsWith("image/"))
                            {
                                elemento.FotoArchivo = await APHelpers.ToBytes(foto);
                            }
                        }

                        _db.Elementos.Add(elemento);
                        await _db.SaveChangesAsync(); 

                        if (item.Detalles != null && item.Detalles.Any())
                        {
                            for (int j = 0; j < item.Detalles.Count; j++)
                            {
                                var det = item.Detalles[j];
                                if (det.IdTipoDetalle <= 0 && string.IsNullOrWhiteSpace(det.Nombre)) continue;

                                int idTipoDetalleFinal = det.IdTipoDetalle;

                                // Si fue escrito manualmente ("Otro"), verificar o crear en TiposDetalle
                                if (idTipoDetalleFinal <= 0 && !string.IsNullOrWhiteSpace(det.Nombre))
                                {
                                    var nombreLimpio = det.Nombre.Trim();
                                    var tipoExistente = await _db.TiposDetalles
                                        .FirstOrDefaultAsync(t => t.Nombre.ToLower() == nombreLimpio.ToLower());

                                    if (tipoExistente != null)
                                    {
                                        idTipoDetalleFinal = tipoExistente.IdTipoDetalle;
                                    }
                                    else
                                    {
                                        var nuevoTipo = new TiposDetalle { Nombre = nombreLimpio };
                                        _db.TiposDetalles.Add(nuevoTipo);
                                        await _db.SaveChangesAsync();
                                        idTipoDetalleFinal = nuevoTipo.IdTipoDetalle;
                                    }
                                }

                                // Obtener la foto del accesorio tomada por el usuario si existe
                                byte[]? fotoAccesorioBytes = null;
                                var fotoAccFile = Request.Form.Files[$"foto_acc_{i}_{j}"];
                                if (fotoAccFile != null && fotoAccFile.Length > 0 && fotoAccFile.ContentType.StartsWith("image/"))
                                {
                                    fotoAccesorioBytes = await APHelpers.ToBytes(fotoAccFile);
                                }

                                var detalleElemento = new DetalleElemento
                                {
                                    IdDetalle = 0,
                                    IdElemento = elemento.IdElemento,
                                    IdTipoDetalle = idTipoDetalleFinal,
                                    FotoArchivo = fotoAccesorioBytes
                                };
                                _db.DetalleElementos.Add(detalleElemento);
                            }

                            await _db.SaveChangesAsync();
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivoParaIngreso([FromForm] int idElemento, [FromForm] bool activo)
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null)
            {
                return Json(new { success = false, message = "Sesión no válida o expirada." });
            }

            var elemento = await _db.Elementos
                .FirstOrDefaultAsync(e => e.IdElemento == idElemento && e.IdUsuario == idUsuario.Value);

            if (elemento == null)
            {
                return Json(new { success = false, message = "Equipo no encontrado o no te pertenece." });
            }

            // Si el equipo se encuentra actualmente dentro de la sede, no se puede desactivar
            if (!activo)
            {
                var tieneMovimientoActivo = await _db.RegistrosMovimientos
                    .AnyAsync(m => m.IdElemento == idElemento && m.FechaSalida == null);

                if (tieneMovimientoActivo)
                {
                    return Json(new { success = false, message = "No puedes desmarcar un equipo mientras se encuentra dentro de la sede." });
                }
            }

            elemento.ActivoParaIngreso = activo;
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                activo = elemento.ActivoParaIngreso,
                message = activo
                    ? "¡Equipo seleccionado! Aparecerá en el panel de control de ingreso en portería."
                    : "Equipo desmarcado. No aparecerá en el control de ingreso hasta que lo vuelvas a seleccionar."
            });
        }

        [HttpGet]
        public async Task<IActionResult> EditarEquipo(int id)
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null) return RedirectToAction("Login", "Auth");

            var elemento = await _db.Elementos
                .Include(e => e.DetalleElementos)
                .FirstOrDefaultAsync(e => e.IdElemento == id && e.IdUsuario == idUsuario.Value);

            if (elemento == null)
            {
                TempData["Error"] = "Equipo no encontrado o no te pertenece.";
                return RedirectToAction(nameof(Index));
            }

            await CargarViewBagsAsync();
            ViewBag.IdElemento = elemento.IdElemento;
            ViewBag.TipoElemento = elemento.TipoElemento;
            ViewBag.IdMarca = elemento.IdMarca;
            ViewBag.Serial = elemento.Serial;
            ViewBag.TieneFoto = elemento.FotoArchivo != null;
            ViewBag.Accesorios = elemento.DetalleElementos.Select(d => d.IdTipoDetalle).ToList();

            return View("~/Views/MiPanel/EditarEquipo.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEquipo(
            int idElemento, string tipoElemento, int? idMarca, string? serial,
            IFormFile? foto, List<int>? accesorios)
        {
            var idUsuario = await ObtenerIdUsuarioSesionAsync();
            if (idUsuario == null) return RedirectToAction("Login", "Auth");

            var elemento = await _db.Elementos
                .Include(e => e.DetalleElementos)
                .FirstOrDefaultAsync(e => e.IdElemento == idElemento && e.IdUsuario == idUsuario.Value);

            if (elemento == null)
            {
                TempData["Error"] = "Equipo no encontrado o no te pertenece.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(tipoElemento))
            {
                TempData["Error"] = "El tipo de equipo es obligatorio.";
                return RedirectToAction(nameof(EditarEquipo), new { id = idElemento });
            }

            elemento.TipoElemento = tipoElemento.Trim();
            elemento.IdMarca = idMarca > 0 ? idMarca : null;
            elemento.Serial = string.IsNullOrWhiteSpace(serial) ? null : serial.Trim();

            if (foto != null && foto.Length > 0 && foto.ContentType.StartsWith("image/"))
            {
                elemento.FotoArchivo = await Helpers.APHelpers.ToBytes(foto);
            }

            // Actualizar accesorios
            _db.DetalleElementos.RemoveRange(elemento.DetalleElementos);

            if (accesorios != null && accesorios.Any())
            {
                foreach (var idTd in accesorios.Distinct())
                {
                    _db.DetalleElementos.Add(new Models.DetalleElemento
                    {
                        IdElemento = idElemento,
                        IdTipoDetalle = idTd
                    });
                }
            }

            try
            {
                await _db.SaveChangesAsync();
                TempData["Exito"] = "¡Equipo actualizado correctamente!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                TempData["Error"] = "Error al guardar: " + msg;
                return RedirectToAction(nameof(EditarEquipo), new { id = idElemento });
            }
        }
    }
}