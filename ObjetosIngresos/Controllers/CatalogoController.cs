using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;

namespace ObjetosIngresos.Controllers
{
    [Authorize(Roles = "Administrador")] 
    public class CatalogosController : Controller
    {
        private readonly CatalogoServices srv;

        public CatalogosController(CatalogoServices catalogoService)
        {
            this.srv = catalogoService;
        }

        // =================================================================
        // VISTA PRINCIPAL: Panel de Control de Catálogos
        // =================================================================
        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            return View("~/Views/Catalogos/Index.cshtml");
        }

        // =================================================================
        // GESTIÓN DE MARCAS
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Marcas()
        {
            var marcas = await srv.GetAllMarcasAsync();
            return View("~/Views/Catalogos/Marcas.cshtml", marcas);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMarca([FromForm] Marca model)
        {
            if (string.IsNullOrEmpty(model.NombreMarca)) return BadRequest("El nombre de la marca es requerido.");

            await srv.AddMarcaAsync(model);
            return RedirectToAction(nameof(Marcas));
        }

        [HttpPost]
        public async Task<IActionResult> EditMarca([FromForm] Marca model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");

            await srv.UpdateMarcaAsync(model);
            return RedirectToAction(nameof(Marcas));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMarca(int id)
        {
            bool eliminado = await srv.DeleteMarcaAsync(id);
            if (!eliminado)
            {
                return Json(new { success = false, message = "No se puede eliminar la marca porque está asociada a elementos del sistema." });
            }
            return Json(new { success = true, message = "Marca eliminada correctamente." });
        }

        // =================================================================
        // GESTIÓN DE TIPOS DETALLE (Periféricos)
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> TiposDetalle()
        {
            var tipos = await srv.GetAllTiposDetalleAsync();
            return View("~/Views/Catalogos/TiposDetalle.cshtml", tipos);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTipoDetalle([FromForm] TiposDetalle model)
        {
            if (string.IsNullOrEmpty(model.Nombre)) return BadRequest("El nombre es requerido.");

            await srv.AddTipoDetalleAsync(model);
            return RedirectToAction(nameof(TiposDetalle));
        }

        [HttpPost]
        public async Task<IActionResult> EditTipoDetalle([FromForm] TiposDetalle model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");

            await srv.UpdateTipoDetalleAsync(model);
            return RedirectToAction(nameof(TiposDetalle));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTipoDetalle(int id)
        {
            bool eliminado = await srv.DeleteTipoDetalleAsync(id);
            if (!eliminado)
            {
                return Json(new { success = false, message = "No se puede eliminar este tipo de detalle; se encuentra en uso activo." });
            }
            return Json(new { success = true, message = "Registro eliminado con éxito." });
        }

        // =================================================================
        // GESTIÓN DE REGIONALES
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Regionales()
        {
            var regionales = await srv.GetAllRegionalesAsync();
            return View("~/Views/Catalogos/Regionales.cshtml", regionales);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRegional([FromForm] Regionale model)
        {
            if (string.IsNullOrEmpty(model.NombreRegional)) return BadRequest("El nombre de la regional es requerido.");

            await srv.AddRegionalAsync(model);
            return RedirectToAction(nameof(Regionales));
        }

        // =================================================================
        // GESTIÓN DE CENTROS DE FORMACIÓN
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> CentrosFormacion()
        {
            var centros = await srv.GetAllCentrosAsync();
            ViewBag.Regionales = await srv.GetAllRegionalesAsync(); 
            return View("~/Views/Catalogos/CentrosFormacion.cshtml", centros);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCentroFormacion([FromForm] CentrosFormacion model)
        {
            if (!ModelState.IsValid) return BadRequest("Información del centro de formación incompleta.");

            await srv.AddCentroFormacionAsync(model);
            return RedirectToAction(nameof(CentrosFormacion));
        }

        // =================================================================
        // GESTIÓN DE SEDES
        // =================================================================
        [HttpGet]
        public async Task<IActionResult> Sedes()
        {
            var sedes = await srv.GetAllSedesAsync();
            ViewBag.CentrosFormacion = await srv.GetAllCentrosAsync(); // Para vincular la sede a un Centro
            return View("~/Views/Catalogos/Sedes.cshtml", sedes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSede([FromForm] Sede model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos de la sede inválidos.");

            await srv.AddSedeAsync(model);
            return RedirectToAction(nameof(Sedes));
        }

        [HttpPost]
        public async Task<IActionResult> EditSede([FromForm] Sede model)
        {
            if (!ModelState.IsValid) return BadRequest("Error al actualizar la sede.");

            await srv.UpdateSedeAsync(model);
            return RedirectToAction(nameof(Sedes));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSede(int id)
        {
            bool eliminado = await srv.DeleteSedeAsync(id);
            if (!eliminado)
            {
                return Json(new { success = false, message = "La sede no puede ser eliminada porque contiene usuarios o registros de movimiento vinculados." });
            }
            return Json(new { success = true, message = "Sede eliminada de forma segura." });
        }

        // =================================================================
        // GESTIÓN DE ROLES / TIPOS DE USUARIO (CRUD)
        // =================================================================

        [HttpGet]
        public async Task<IActionResult> TiposUsuario()
        {
            var roles = await srv.GetAllTiposUsuarioAsync();
            return View("~/Views/Catalogos/TiposUsuario.cshtml", roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTipoUsuario(string NombreTipo)
        {
            if (string.IsNullOrWhiteSpace(NombreTipo))
            {
                return RedirectToAction("TiposUsuario");
            }

            var nuevoRol = new TiposUsuario { Descripcion = NombreTipo.Trim() };
            await srv.CreateTipoUsuarioAsync(nuevoRol);

            return RedirectToAction("TiposUsuario");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTipoUsuario(int IdTipoUsuario, string NombreTipo)
        {
            if (string.IsNullOrWhiteSpace(NombreTipo))
            {
                return Json(new { success = false, message = "El nombre del rol no puede estar vacío." });
            }

            var rol = new TiposUsuario
            {
                IdTipoUsuario = IdTipoUsuario,
                Descripcion = NombreTipo.Trim()
            };

            bool resultado = await srv.UpdateTipoUsuarioAsync(rol);
            if (resultado)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "No se pudo actualizar el rol en la base de datos." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTipoUsuario(int id)
        {
            bool resultado = await srv.DeleteTipoUsuarioAsync(id);
            if (resultado)
            {
                return Json(new { success = true });
            }

            return Json(new
            {
                success = false,
                message = "No se puede eliminar este rol porque se encuentra asignado a usuarios activos."
            });
        }
    }
}