using Microsoft.AspNetCore.Mvc;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;

namespace ObjetosIngresos.Controllers
{
    public class ElementosController:Controller
    {
        private readonly ElementoServices srvElemento;
        private readonly CatalogoServices srvCatalogos; 

        public ElementosController(ElementoServices _srvElemento, CatalogoServices _srvCatalogos)
        {
            srvElemento = _srvElemento;
            srvCatalogos = _srvCatalogos;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var elementos = await srvElemento.GetAllOptimizadoAsync();
            return View("~/Views/Elemento/Index.cshtml", elementos);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Marcas = await srvCatalogos.GetAllMarcasAsync();
            ViewBag.TiposDetalle = await srvCatalogos.GetAllTiposDetalleAsync(); 

            return View("~/Views/Elemento/Create.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Elemento nuevoElemento, List<DetalleElemento> detalles, IFormFile? foto)
        {
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("detalles")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(nuevoElemento.TipoElemento))
                {
                    ModelState.AddModelError("TipoElemento", "El tipo o descripción del elemento es obligatorio.");
                }

                if (ModelState.IsValid)
                {
                    await srvElemento.AddAsync(nuevoElemento, detalles, foto);
                    TempData["Success"] = "¡Equipo registrado correctamente en el inventario!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error en la Base de Datos: {ex.Message} -> {ex.InnerException?.Message}");
            }

            ViewBag.Marcas = await srvCatalogos.GetAllMarcasAsync();
            ViewBag.TiposDetalle = await srvCatalogos.GetAllTiposDetalleAsync();
            return View("~/Views/Elemento/Create.cshtml", nuevoElemento);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var elemento = await srvElemento.GetByIdAsync(id);
            if (elemento == null)
            {
                return NotFound();
            }

            ViewBag.Marcas = await srvCatalogos.GetAllMarcasAsync();
            ViewBag.TiposDetalle = await srvCatalogos.GetAllTiposDetalleAsync();
            return View("~/Views/Elemento/Edit.cshtml", elemento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Elemento elementoActualizado, List<DetalleElemento> detalles, IFormFile? foto)
        {
            var keysToRemove = ModelState.Keys.Where(k =>
                k.StartsWith("detalles") ||
                k.Contains("Navigation") ||
                k == "FotoArchivo" ||
                k == "IdUsuario" ||
                k == "RegistrosMovimientos" ||
                k == "DetalleElementos"
            ).ToList();

            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await srvElemento.UpdateAsync(elementoActualizado, detalles, foto);
                    TempData["Success"] = "¡Equipo actualizado correctamente en el inventario!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el registro: {ex.Message} -> {ex.InnerException?.Message}");
            }

            ViewBag.Marcas = await srvCatalogos.GetAllMarcasAsync();
            ViewBag.TiposDetalle = await srvCatalogos.GetAllTiposDetalleAsync();
            return View("~/Views/Elemento/Edit.cshtml", elementoActualizado);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id) 
        {
            try
            {
                await srvElemento.DeleteAsync(id);
                return Json(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "No se pudo eliminar el equipo: " + (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        [HttpGet]
        [Route("Elementos/ObtenerImagen/{id}")]
        [ResponseCache(Duration = 86400)] // Caché de 24 horas
        public async Task<IActionResult> ObtenerImagen(int id)
        {
            var elemento = await srvElemento.GetByIdAsync(id);
            if (elemento?.FotoArchivo != null && elemento.FotoArchivo.Length > 0)
            {
                return File(elemento.FotoArchivo, "image/jpeg");
            }
            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(CancellationToken cancellationToken)
        {
            try
            {
                var excelBytes = await srvElemento.ExportarElementosAExcelAsync();
                string fileName = $"Inventario_Elementos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (OperationCanceledException)
            {
                return Empty;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al generar el archivo de exportación.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}

