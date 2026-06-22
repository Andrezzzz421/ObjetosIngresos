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
        public IActionResult Index()
        {
            var elementos = srvElemento.GetAll();
            return View("~/Views/Catalogos/ElementosIndex.cshtml", elementos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Marcas = srvCatalogos.GetAllMarcas();
            ViewBag.TiposDetalle = srvCatalogos.GetAllTiposDetalle(); 

            return View("~/Views/Catalogos/CreateElemento.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Elemento nuevoElemento, List<DetalleElemento> detalles, IFormFile? foto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nuevoElemento.TipoElemento))
                {
                    ModelState.AddModelError("TipoElemento", "El tipo o descripción del elemento es obligatorio.");
                }

                if (ModelState.IsValid)
                {
                    srvElemento.Add(nuevoElemento, detalles, foto);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error interno al guardar: {ex.Message}");
            }
            ViewBag.Marcas = srvCatalogos.GetAllMarcas();
            ViewBag.TiposDetalle = srvCatalogos.GetAllTiposDetalle();
            return View("~/Views/Catalogos/CreateElemento.cshtml", nuevoElemento);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var elemento = srvElemento.GetById(id);
            if (elemento == null)
            {
                return NotFound();
            }

            ViewBag.Marcas = srvCatalogos.GetAllMarcas();
            ViewBag.TiposDetalle = srvCatalogos.GetAllTiposDetalle();
            return View("~/Views/Catalogos/EditElemento.cshtml", elemento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Elemento elementoActualizado, List<DetalleElemento> detalles, IFormFile? foto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    srvElemento.Update(elementoActualizado, detalles, foto);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar el registro: {ex.Message}");
            }

            ViewBag.Marcas = srvCatalogos.GetAllMarcas();
            ViewBag.TiposDetalle = srvCatalogos.GetAllTiposDetalle();
            return View("~/Views/Catalogos/EditElemento.cshtml", elementoActualizado);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                srvElemento.Delete(id);
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
    }
}

