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
        private readonly ImportacionServices sser;

        public UsuariosController(SistemaIngresoContext db, UsuarioServices ser,ImportacionServices sr)
        {
            _db = db;
            _ser = ser;
            sser = sr;

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
                return Json($"El nï¿½mero de documento '{documento}' ya estï¿½ registrado.");
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
                return Json($"El correo electrï¿½nico '{correo}' ya estï¿½ registrado.");
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

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportarExcel(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                TempData["Error"] = "Por favor, selecciona un archivo Excel válido.";
                return RedirectToAction("Index"); // O la vista donde tengas la tabla de usuarios
            }

            try
            {
                var (registrados, errores) = await sser.ImportarUsuariosDesdeExcel(archivoExcel);

                if (registrados > 0)
                {
                    TempData["Success"] = $"Se importaron exitosamente {registrados} usuarios.";
                }

                if (errores != null && errores.Count > 0)
                {
                    // Si hubo errores por filas duplicadas o datos faltantes, los mostramos
                    TempData["Advertencias"] = string.Join("<br/>", errores);
                }
                else if (registrados == 0)
                {
                    TempData["Error"] = "No se pudo importar ningún usuario. Revisa el formato del archivo.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al procesar el archivo: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}
