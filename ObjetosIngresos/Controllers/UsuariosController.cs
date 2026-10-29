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
    [Authorize(Roles = "SuperAdmin")]
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _ser.GetAllAsync();
            return View(usuarios);
        }

        [HttpGet]
        public ActionResult Create()
        {
            CargarCombos();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    await _ser.Add(us);
                    TempData["Success"] = "Usuario creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            CargarCombos(us);
            return View(us);
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var user = _ser.GetById(id);
            if (user == null)
            {
                return Content($"Error: El usuario con ID {id} no existe.");
            }

            CargarCombos(user);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    await _ser.Update(us);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            CargarCombos(us);
            return View(us);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarDocumento(string documento, int idUsuario = 0)
        {
            bool existe = await _ser.ExisteDocumento(documento, idUsuario);
            if (existe)
            {
                return Json($"El n�mero de documento '{documento}' ya est� registrado.");
            }
            return Json(true);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarCorreo(string correo, int idUsuario = 0)
        {
            bool existe = await _ser.ExisteCorreo(correo, idUsuario);
            if (existe)
            {
                return Json($"El correo electr�nico '{correo}' ya est� registrado.");
            }
            return Json(true);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(CancellationToken cancellationToken)
        {
            try
            {
                var bytes = await _ser.ExportarUsuariosAExcelAsync();
                string fileName = $"Usuarios_Registrados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (OperationCanceledException)
            {
                return Empty;
            }
            catch (Exception)
            {
                TempData["Error"] = "No se pudo generar el reporte de usuarios.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
