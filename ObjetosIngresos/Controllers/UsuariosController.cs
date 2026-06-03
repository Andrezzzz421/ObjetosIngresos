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

        private readonly SistemaIngresoContext db;
        private readonly UsuarioServices ser;

        public UsuariosController(SistemaIngresoContext db)
        {
            this.db = db;
            ser = new UsuarioServices(db);
        }

        private void CargarCombos(Usuario u = null)
        {
            ViewBag.Sedes = new SelectList(db.Sedes, "IdSede", "NombreSede", u?.IdSedePrincipal);
            ViewBag.TiposUsuarios = new SelectList(db.TiposUsuarios, "IdTipoUsuario", "Descripcion", u?.IdTipoUsuario);
        }
        public ActionResult Index()
        {
            return View(ser.GetAll());
        }

        public ActionResult Create()
        {
            CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Usuario us)
        {
            if (ModelState.IsValid)
            {
                ser.Add(us);
                return RedirectToAction(nameof(Index));
            }
            return View(us);
        }

        public ActionResult Edit(int id)
        {
            var user = ser.GetById(id);

            if (user == null)
            {
                return Content($"Error: El usuario con ID {id} no existe en la base de datos.");
            }
            CargarCombos();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");
            try
            {

                if (ModelState.IsValid)
                {
                    ser.Update(us);
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
        public ActionResult Delete(int id)
        {
            bool eliminado = ser.Delete(id);

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
      
