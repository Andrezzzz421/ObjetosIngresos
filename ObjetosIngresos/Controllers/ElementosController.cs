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
            var elementos = await srvElemento.GetAllAsync();
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
            var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("detalles")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await srvElemento.UpdateAsync(elementoActualizado, detalles, foto);
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
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "No se puede eliminar el equipo. Verifique si está asociado a un registro de movimientos/ingresos activo."
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
    }
}

