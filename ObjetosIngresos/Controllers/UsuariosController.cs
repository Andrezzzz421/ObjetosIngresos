using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

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

        private async Task CargarCombosAsync(Usuario? u = null)
        {
            var sedes = await _db.Sedes.AsNoTracking().ToListAsync();
            var tiposUsuarios = await _db.TiposUsuarios.AsNoTracking().ToListAsync();

            ViewBag.Sedes = new SelectList(sedes, "IdSede", "NombreSede", u?.IdSedePrincipal);
            ViewBag.TiposUsuarios = new SelectList(tiposUsuarios, "IdTipoUsuario", "Descripcion", u?.IdTipoUsuario);
        }

        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Aprendiz"))
            {
                return RedirectToAction("Perfil", "Auth");
            }

            var usuarios = await _ser.GetAll();
            return View(usuarios);
        }

        [Authorize(Roles = "Administrador,Instructor")]
        public async Task<IActionResult> Create()
        {
            await CargarCombosAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Instructor")]
        public async Task<IActionResult> Create(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    await _ser.Add(us);
                    TempData["Success"] = "Usuario creado con éxito.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ocurrió un error al guardar el usuario: " + ex.Message);
                }
            }

            await CargarCombosAsync(us);
            return View(us);
        }

        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _ser.GetById(id);
            if (user == null)
            {
                TempData["Error"] = $"El usuario con ID {id} no fue encontrado.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("Aprendiz"))
            {
                var documentoLogueado = User.FindFirst("Documento")?.Value;
                if (user.Documento != documentoLogueado)
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            await CargarCombosAsync(user);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Instructor,Aprendiz")]
        public async Task<IActionResult> Edit(Usuario us)
        {
            ModelState.Remove("IdSedePrincipalNavigation");
            ModelState.Remove("IdTipoUsuarioNavigation");

            if (User.IsInRole("Aprendiz"))
            {
                var documentoLogueado = User.FindFirst("Documento")?.Value;
                var usuarioOriginal = await _ser.GetById(us.IdUsuario);

                if (usuarioOriginal == null || usuarioOriginal.Documento != documentoLogueado)
                {
                    return Forbid();
                }

                // Protegemos campos no editables por un Aprendiz
                us.IdTipoUsuario = usuarioOriginal.IdTipoUsuario;
                us.IdSedePrincipal = usuarioOriginal.IdSedePrincipal;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _ser.Update(us);
                    TempData["Success"] = "Datos actualizados correctamente.";

                    if (User.IsInRole("Aprendiz"))
                        return RedirectToAction("Perfil", "Auth");

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al actualizar el usuario: " + ex.Message);
                }
            }

            await CargarCombosAsync(us);
            return View(us);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            bool eliminado = await _ser.Delete(id);

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