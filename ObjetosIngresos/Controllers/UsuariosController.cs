using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly SistemaIngresoContext _db;
        private readonly UsuarioServices _ser;

        public UsuariosController(SistemaIngresoContext db, UsuarioServices ser)
        {
            _db = db;
            _ser = ser;
        }

        private void CargarCombos(Usuario? u = null)
        {
            ViewBag.Sedes = new SelectList(_db.Sedes, "IdSede", "NombreSede", u?.IdSedePrincipal);
            ViewBag.TiposUsuarios = new SelectList(_db.TiposUsuarios, "IdTipoUsuario", "Descripcion", u?.IdTipoUsuario);
        }

        /// <summary>
        /// Vista Principal: Regula qué ve cada rol para evitar fugas de información.
        /// </summary>
        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public ActionResult Index()
        {
            // 💡 Si es Aprendiz, lo mandamos directo al Perfil de Auth. 
            // ¡Adiós código duplicado y adiós método PerfilPropio!
            if (User.IsInRole("Aprendiz"))
            {
                return RedirectToAction("Perfil", "Auth");
            }

            // Administradores e Instructores ven el listado global sin problemas
            return View(_ser.GetAll());
        }

        /// <summary>
        /// Crear Usuario: Solo personal administrativo o encargados.
        /// </summary>
        [Authorize(Roles = "Administrador,Instructor")]
        public ActionResult Create()
        {
            CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Instructor")]
        public ActionResult Create(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (ModelState.IsValid)
            {
                _ser.Add(us);
                return RedirectToAction(nameof(Index));
            }
            CargarCombos(us);
            return View(us);
        }

        /// <summary>
        /// Editar Usuario: El Administrador/Instructor edita a cualquiera. El Aprendiz solo a sí mismo.
        /// </summary>
        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public ActionResult Edit(int id)
        {
            var user = _ser.GetById(id);
            if (user == null)
            {
                return Content($"Error: El usuario con ID {id} no existe.");
            }

            if (User.IsInRole("Aprendiz"))
            {
                var documentoLogueado = User.FindFirst("Documento")?.Value;
                if (user.Documento != documentoLogueado)
                {
                    return RedirectToAction("AccessDenied", "Auth"); 
                }
            }

            CargarCombos(user);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public ActionResult Edit(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (User.IsInRole("Aprendiz"))
            {
                var documentoLogueado = User.FindFirst("Documento")?.Value;
                var usuarioOriginal = _ser.GetById(us.IdUsuario);

                if (usuarioOriginal == null || usuarioOriginal.Documento != documentoLogueado)
                {
                    return Forbid();
                }

                us.IdTipoUsuario = usuarioOriginal.IdTipoUsuario;
                us.IdSedePrincipal = usuarioOriginal.IdSedePrincipal;
            }

            try
            {
                if (ModelState.IsValid)
                {
                    _ser.Update(us);

                    if (User.IsInRole("Aprendiz"))
                        return RedirectToAction("Perfil", "Auth");

                    return RedirectToAction(nameof(Index));
                }
                CargarCombos(us);
                return View(us);
            }
            catch
            {
                CargarCombos(us);
                return View(us);
            }
        }

        /// <summary>
        /// Eliminar Usuario: Operación de máxima criticidad. SÓLO el Administrador puede ejecutarla.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public ActionResult Delete(int id)
        {
            bool eliminado = _ser.Delete(id);

            if (eliminado)
            {
                TempData["Success"] = "El usuario ha sido eliminado correctamente.";
            }
            else
            {
                TempData["Error"] = "No se pudo eliminar: el usuario tiene registros vinculados o no existe.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}