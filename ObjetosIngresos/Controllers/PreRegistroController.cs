using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using ObjetosIngresos.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ObjetosIngresos.Controllers
{
    [AllowAnonymous]
    public class PreRegistroController : Controller
    {
        private readonly SistemaIngresoContext _context;
        private readonly AuthServices _authService;
        private readonly IMemoryCache _cache;

        public PreRegistroController(
            SistemaIngresoContext context,
            AuthServices authService,
            IMemoryCache cache)
        {
            _context = context;
            _authService = authService;
            _cache = cache;
        }

        private static string ObtenerCacheKey(string correo) => $"PreReg_{correo.Trim().ToLower()}";

        // GET: /PreRegistro
        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/PreRegistro/Index.cshtml");
        }

        // POST: /PreRegistro/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(PreRegistroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/PreRegistro/Index.cshtml", model);
            }

            var docLimpio = model.Documento.Trim();
            var correoLimpio = model.Correo.Trim().ToLower();

            // 1. Validar si ya existe un usuario registrado con el mismo número de documento
            var existeDoc = await _context.Usuarios.AnyAsync(u => u.Documento == docLimpio);
            if (existeDoc)
            {
                ModelState.AddModelError("Documento", "El número de documento ingresado ya se encuentra registrado en el sistema.");
                return View("~/Views/PreRegistro/Index.cshtml", model);
            }

            // 2. Validar si ya existe un usuario registrado con el mismo correo electrónico
            var existeCorreo = await _context.Usuarios.AnyAsync(u => u.Correo == correoLimpio);
            if (existeCorreo)
            {
                ModelState.AddModelError("Correo", "El correo electrónico ingresado ya se encuentra registrado en el sistema.");
                return View("~/Views/PreRegistro/Index.cshtml", model);
            }

            // 3. Generar código de verificación de 6 dígitos numéricos
            string codigoGenerado = new Random().Next(100000, 999999).ToString();

            // 4. Guardar datos en memoria temporal (expira en 15 minutos)
            var datosTemporales = new PreRegistroDatosTemporales
            {
                Documento = docLimpio,
                Nombres = model.Nombres.Trim(),
                Apellidos = model.Apellidos.Trim(),
                Correo = correoLimpio,
                Codigo = codigoGenerado,
                ExpiracionUtc = DateTime.UtcNow.AddMinutes(15)
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };

            _cache.Set(ObtenerCacheKey(correoLimpio), datosTemporales, cacheOptions);

            // 5. Enviar el correo con el código
            try
            {
                await _authService.EnviarCodigoPreRegistroAsync(correoLimpio, model.Nombres.Trim(), codigoGenerado);
            }
            catch (Exception)
            {
                _cache.Remove(ObtenerCacheKey(correoLimpio));
                ModelState.AddModelError(string.Empty, "No fue posible enviar el código de verificación al correo ingresado. Por favor, asegúrate de que el correo esté bien escrito o inténtalo nuevamente más tarde.");
                return View("~/Views/PreRegistro/Index.cshtml", model);
            }

            TempData["MensajeInfo"] = $"Hemos enviado un código de verificación de 6 dígitos a {correoLimpio}.";
            return RedirectToAction(nameof(VerificarCodigo), new { email = correoLimpio });
        }

        // GET: /PreRegistro/VerificarCodigo?email=...
        [HttpGet]
        public IActionResult VerificarCodigo(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(Index));
            }

            var correoLimpio = email.Trim().ToLower();
            if (!_cache.TryGetValue(ObtenerCacheKey(correoLimpio), out PreRegistroDatosTemporales? datos))
            {
                TempData["Error"] = "Tu sesión de registro ha expirado o no existe. Por favor, ingresa tus datos nuevamente.";
                return RedirectToAction(nameof(Index));
            }

            var model = new PreRegistroCodigoViewModel
            {
                Correo = correoLimpio
            };

            return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
        }

        // POST: /PreRegistro/ValidarCodigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarCodigo(PreRegistroCodigoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
            }

            var correoLimpio = model.Correo.Trim().ToLower();
            var cacheKey = ObtenerCacheKey(correoLimpio);

            if (!_cache.TryGetValue(cacheKey, out PreRegistroDatosTemporales? temp) || temp == null)
            {
                ModelState.AddModelError(string.Empty, "El código ha expirado o la sesión de verificación ya no es válida. Por favor, solicita un nuevo código o inicia tu registro otra vez.");
                return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
            }

            if (temp.Codigo != model.Codigo.Trim())
            {
                ModelState.AddModelError("Codigo", "El código de verificación ingresado es incorrecto. Por favor, revisa tu bandeja de entrada o spam.");
                return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
            }

            // Validar nuevamente que el documento o correo no hayan sido registrados mientras tanto
            if (await _context.Usuarios.AnyAsync(u => u.Documento == temp.Documento))
            {
                _cache.Remove(cacheKey);
                ModelState.AddModelError(string.Empty, "El documento ya fue registrado en el sistema.");
                return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
            }

            if (await _context.Usuarios.AnyAsync(u => u.Correo == temp.Correo))
            {
                _cache.Remove(cacheKey);
                ModelState.AddModelError(string.Empty, "El correo electrónico ya fue registrado en el sistema.");
                return View("~/Views/PreRegistro/VerificarCodigo.cshtml", model);
            }

            // Obtener el rol de Visitante
            var idRolVisitante = await _context.TiposUsuarios
                .Where(t => t.Descripcion.ToLower().Contains("visitante"))
                .Select(t => t.IdTipoUsuario)
                .FirstOrDefaultAsync();

            if (idRolVisitante == 0) idRolVisitante = 4;

            var usuario = new Usuario
            {
                Documento = temp.Documento.Trim(),
                Nombres = temp.Nombres.Trim(),
                Apellidos = temp.Apellidos.Trim(),
                Correo = temp.Correo.Trim(),
                IdTipoUsuario = idRolVisitante
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            // Limpiar la caché tras éxito
            _cache.Remove(cacheKey);

            // Datos para la vista de éxito
            TempData["ExitoPreRegistro"] = true;
            TempData["UsuarioNombre"] = $"{usuario.Nombres} {usuario.Apellidos}";
            TempData["UsuarioDocumento"] = usuario.Documento;
            TempData["UsuarioCorreo"] = usuario.Correo;
            TempData["FechaRegistro"] = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            return RedirectToAction(nameof(Exito));
        }

        // POST: /PreRegistro/ReenviarCodigo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarCodigo(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "El correo electrónico es requerido." });
            }

            var correoLimpio = email.Trim().ToLower();
            var cacheKey = ObtenerCacheKey(correoLimpio);

            if (!_cache.TryGetValue(cacheKey, out PreRegistroDatosTemporales? temp) || temp == null)
            {
                return Json(new { success = false, message = "Tu sesión ha expirado. Por favor regresa e ingresa tus datos de nuevo." });
            }

            string nuevoCodigo = new Random().Next(100000, 999999).ToString();
            temp.Codigo = nuevoCodigo;
            temp.ExpiracionUtc = DateTime.UtcNow.AddMinutes(15);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
            };

            _cache.Set(cacheKey, temp, cacheOptions);

            try
            {
                await _authService.EnviarCodigoPreRegistroAsync(correoLimpio, temp.Nombres, nuevoCodigo);
                return Json(new { success = true, message = $"Hemos reenviado un nuevo código de 6 dígitos a {correoLimpio}." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Ocurrió un error al intentar reenviar el correo. Por favor intenta más tarde." });
            }
        }

        // GET: /PreRegistro/Exito
        [HttpGet]
        public IActionResult Exito()
        {
            if (TempData["ExitoPreRegistro"] == null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.NombreCompleto = TempData["UsuarioNombre"]?.ToString() ?? "Usuario";
            ViewBag.Documento = TempData["UsuarioDocumento"]?.ToString() ?? "";
            ViewBag.Correo = TempData["UsuarioCorreo"]?.ToString() ?? "";
            ViewBag.FechaRegistro = TempData["FechaRegistro"]?.ToString() ?? DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

            return View("~/Views/PreRegistro/Exito.cshtml");
        }
    }
}