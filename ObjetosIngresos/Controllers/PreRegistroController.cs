using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
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
            ViewBag.Marcas = new SelectList(await _context.Marcas.ToListAsync(), "IdMarca", "NombreMarca");
            return View();
        }

        // POST: /PreRegistro/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PreRegistroViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Documento == model.Documento);

                if (usuario == null)
                {
                    usuario = new Usuario
                    {
                        Documento = model.Documento,
                        Nombres = model.Nombres,
                        Apellidos = model.Apellidos,
                        Correo = model.Correo,
                        IdTipoUsuario = 4 // ID para Visitante
                    };
                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();
                }

                var elemento = new Elemento
                {
                    IdUsuario = usuario.IdUsuario,
                    TipoElemento = model.TipoElemento,
                    IdMarca = model.IdMarca,
                    Serial = model.Serial
                };
                _context.Elementos.Add(elemento);
                await _context.SaveChangesAsync();

                return View("Exito");
            }

            ViewBag.Marcas = new SelectList(await _context.Marcas.ToListAsync(), "IdMarca", "NombreMarca", model.IdMarca);
            return View("Index", model);
        }
    }
}
