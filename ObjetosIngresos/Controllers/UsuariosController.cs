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
    //[Authorize]
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
            ViewBag.Sedes = new SelectList(db.Sedes, "IdSede", "Nombre", u?.IdSedePrincipal);
            ViewBag.TipoUsuarios = new SelectList(db.TiposUsuarios, "IdTipoUsuario", "Descripcion", u?.IdTipoUsuario);
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
                NotFound();
            }
            CargarCombos();
            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Usuario us)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    ser.Update(us);
                    return RedirectToAction(nameof(Index));
                }
                CargarCombos();
                return View(ser.GetAll());
            }
            catch
            {
                return View();
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
      
