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
        public IActionResult Marcas()
        {
            var marcas = srv.GetAllMarcas();
            return View("~/Views/Catalogos/Marcas.cshtml", marcas);
        }

        [HttpPost]
        public IActionResult CreateMarca([FromForm] Marca model)
        {
            if (string.IsNullOrEmpty(model.NombreMarca)) return BadRequest("El nombre de la marca es requerido.");

            srv.AddMarca(model);
            return RedirectToAction(nameof(Marcas));
        }

        [HttpPost]
        public IActionResult EditMarca([FromForm] Marca model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");

            srv.UpdateMarca(model);
            return RedirectToAction(nameof(Marcas));
        }

        [HttpPost]
        public IActionResult DeleteMarca(int id)
        {
            bool eliminado = srv.DeleteMarca(id);
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
        public IActionResult TiposDetalle()
        {
            var tipos = srv.GetAllTiposDetalle();
            return View("~/Views/Catalogos/TiposDetalle.cshtml", tipos);
        }

        [HttpPost]
        public IActionResult CreateTipoDetalle([FromForm] TiposDetalle model)
        {
            if (string.IsNullOrEmpty(model.Nombre)) return BadRequest("El nombre es requerido.");

            srv.AddTipoDetalle(model);
            return RedirectToAction(nameof(TiposDetalle));
        }

        [HttpPost]
        public IActionResult EditTipoDetalle([FromForm] TiposDetalle model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos inválidos.");

            srv.UpdateTipoDetalle(model);
            return RedirectToAction(nameof(TiposDetalle));
        }

        [HttpPost]
        public IActionResult DeleteTipoDetalle(int id)
        {
            bool eliminado = srv.DeleteTipoDetalle(id);
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
        public IActionResult Regionales()
        {
            var regionales = srv.GetAllRegionales();
            return View("~/Views/Catalogos/Regionales.cshtml", regionales);
        }

        [HttpPost]
        public IActionResult CreateRegional([FromForm] Regionale model)
        {
            if (string.IsNullOrEmpty(model.NombreRegional)) return BadRequest("El nombre de la regional es requerido.");

            srv.AddRegional(model);
            return RedirectToAction(nameof(Regionales));
        }

        // =================================================================
        // GESTIÓN DE CENTROS DE FORMACIÓN
        // =================================================================
        [HttpGet]
        public IActionResult CentrosFormacion()
        {
            var centros = srv.GetAllCentros();
            ViewBag.Regionales = srv.GetAllRegionales(); 
            return View("~/Views/Catalogos/CentrosFormacion.cshtml", centros);
        }

        [HttpPost]
        public IActionResult CreateCentroFormacion([FromForm] CentrosFormacion model)
        {
            if (!ModelState.IsValid) return BadRequest("Información del centro de formación incompleta.");

            srv.AddCentroFormacion(model);
            return RedirectToAction(nameof(CentrosFormacion));
        }

        // =================================================================
        // GESTIÓN DE SEDES
        // =================================================================
        [HttpGet]
        public IActionResult Sedes()
        {
            var sedes = srv.GetAllSedes();
            ViewBag.CentrosFormacion = srv.GetAllCentros(); // Para vincular la sede a un Centro
            return View("~/Views/Catalogos/Sedes.cshtml", sedes);
        }

        [HttpPost]
        public IActionResult CreateSede([FromForm] Sede model)
        {
            if (!ModelState.IsValid) return BadRequest("Datos de la sede inválidos.");

            srv.AddSede(model);
            return RedirectToAction(nameof(Sedes));
        }

        [HttpPost]
        public IActionResult EditSede([FromForm] Sede model)
        {
            if (!ModelState.IsValid) return BadRequest("Error al actualizar la sede.");

            srv.UpdateSede(model);
            return RedirectToAction(nameof(Sedes));
        }

        [HttpPost]
        public IActionResult DeleteSede(int id)
        {
            bool eliminado = srv.DeleteSede(id);
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
        public IActionResult TiposUsuario()
        {
            var roles = srv.GetAllTiposUsuario();
            return View("~/Views/Catalogos/TiposUsuario.cshtml", roles);
        }

        [HttpPost]
        public IActionResult CreateTipoUsuario(string NombreTipo)
        {
            if (string.IsNullOrWhiteSpace(NombreTipo))
            {
                return RedirectToAction("TiposUsuario");
            }

            var nuevoRol = new TiposUsuario { Descripcion = NombreTipo.Trim() };
            srv.CreateTipoUsuario(nuevoRol);

            return RedirectToAction("TiposUsuario");
        }

        [HttpPost]
        public IActionResult UpdateTipoUsuario(int IdTipoUsuario, string NombreTipo)
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

            bool resultado = srv.UpdateTipoUsuario(rol);
            if (resultado)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "No se pudo actualizar el rol en la base de datos." });
        }

        [HttpPost]
        public IActionResult DeleteTipoUsuario(int id)
        {
            bool resultado = srv.DeleteTipoUsuario(id);
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