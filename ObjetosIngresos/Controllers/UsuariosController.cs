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

        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public ActionResult Index()
        {
            if (User.IsInRole("Aprendiz"))
            {
                return RedirectToAction("Perfil", "Auth");
            }

            return View(_ser.GetAll());
        }
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