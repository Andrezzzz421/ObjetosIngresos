using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.ViewModel;
using System.Linq;
using System.Threading.Tasks;

namespace ObjetosIngresos.Controllers
{
    [AllowAnonymous]
    public class PreRegistroController : Controller
    {
        private readonly SistemaIngresoContext _context;

        public PreRegistroController(SistemaIngresoContext context)
        {
            _context = context;
        }

        // GET: /PreRegistro
        public async Task<IActionResult> Index()
        {
            ViewBag.Marcas = new SelectList(await _context.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca");
            return View("~/Views/PreRegistro/Index.cshtml");
        }

        // POST: /PreRegistro/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PreRegistroViewModel model)
        {
            ModelState.Remove("TipoElemento");
            ModelState.Remove("Serial");

            if (ModelState.IsValid)
            {
                // Validar si ya existe un usuario registrado con el mismo número de documento
                var usuarioExistente = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Documento == model.Documento.Trim());

                if (usuarioExistente != null)
                {
                    // Agregamos un error al ModelState para que se muestre en la vista y detenemos el proceso
                    ModelState.AddModelError("Documento", "El número de documento ingresado ya se encuentra registrado en el sistema.");

                    ViewBag.Marcas = new SelectList(await _context.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca", model.IdMarca);
                    return View("~/Views/PreRegistro/Index.cshtml", model);
                }

                // Si no existe, se procede a crearlo como visitante
                var idRolVisitante = await _context.TiposUsuarios
                    .Where(t => t.Descripcion.ToLower().Contains("visitante"))
                    .Select(t => t.IdTipoUsuario)
                    .FirstOrDefaultAsync();

                if (idRolVisitante == 0) idRolVisitante = 4;

                var usuario = new Usuario
                {
                    Documento = model.Documento.Trim(),
                    Nombres = model.Nombres.Trim(),
                    Apellidos = model.Apellidos.Trim(),
                    Correo = model.Correo.Trim(),
                    IdTipoUsuario = idRolVisitante
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                ViewBag.Propietario = $"{usuario.Nombres} {usuario.Apellidos}";

                return View("~/Views/PreRegistro/Exito.cshtml");
            }

            ViewBag.Marcas = new SelectList(await _context.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca", model.IdMarca);
            return View("~/Views/PreRegistro/Index.cshtml", model);
        }
    }
}