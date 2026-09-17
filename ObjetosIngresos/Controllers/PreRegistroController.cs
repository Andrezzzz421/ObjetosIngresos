using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.ViewModel;
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
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Documento == model.Documento.Trim());

                if (usuario == null)
                {
                    var idRolVisitante = await _context.TiposUsuarios
                        .Where(t => t.Descripcion.ToLower().Contains("visitante"))
                        .Select(t => t.IdTipoUsuario)
                        .FirstOrDefaultAsync();

                    if (idRolVisitante == 0) idRolVisitante = 4;

                    usuario = new Usuario
                    {
                        Documento = model.Documento.Trim(),
                        Nombres = model.Nombres.Trim(),
                        Apellidos = model.Apellidos.Trim(),
                        Correo = model.Correo.Trim(),
                        IdTipoUsuario = idRolVisitante
                    };
                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();
                }

                var elemento = new Elemento
                {
                    IdUsuario = usuario.IdUsuario,
                    TipoElemento = model.TipoElemento.Trim(),
                    IdMarca = model.IdMarca,
                    Serial = model.Serial?.Trim()
                };
                _context.Elementos.Add(elemento);
                await _context.SaveChangesAsync();

                ViewBag.Propietario = $"{usuario.Nombres} {usuario.Apellidos}";
                ViewBag.Equipo = elemento.TipoElemento;
                ViewBag.Serial = elemento.Serial ?? "Sin serial";

                return View("~/Views/PreRegistro/Exito.cshtml");
            }

            ViewBag.Marcas = new SelectList(await _context.Marcas.OrderBy(m => m.NombreMarca).ToListAsync(), "IdMarca", "NombreMarca", model.IdMarca);
            return View("~/Views/PreRegistro/Index.cshtml", model);
        }
    }
}
