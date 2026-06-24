using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjetosIngresos.Services;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthServices _authService;
        private readonly IConfiguration _config;

        public AuthController(AuthServices authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
        }

        private void CargarConfiguracionFirebase()
        {
            ViewBag.FirebaseConfig = new
            {
                apiKey = _config["FirebaseConfig:ApiKey"],
                authDomain = _config["FirebaseConfig:AuthDomain"],
                projectId = _config["FirebaseConfig:ProjectId"],
                storageBucket = _config["FirebaseConfig:StorageBucket"],
                messagingSenderId = _config["FirebaseConfig:MessagingSenderId"],
                appId = _config["FirebaseConfig:AppId"]
            };
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Guest"))
            {
                return RedirectToAction("Perfil");
            }

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Guest"))
            {
                HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            }

            CargarConfiguracionFirebase();
            return View("~/Views/Usuarios/Login.cshtml");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string identificador, string password) // Reemplaza por tu ViewModel si usas uno
        {
            // 1. Validación básica de campos vacíos
            if (string.IsNullOrEmpty(identificador) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "El usuario y la contraseña son obligatorios.");
                CargarConfiguracionFirebase();
                return View("~/Views/Usuarios/Login.cshtml");
            }

            try
            {
                // 2. Intentar buscar al usuario en la base de datos usando tu servicio
                var usuario = await _authService.BuscarUsuarioPorIdOEmailAsync(identificador);

                if (usuario == null)
                {
                    ModelState.AddModelError(string.Empty, "El usuario no se encuentra registrado.");
                    CargarConfiguracionFirebase();
                    return View("~/Views/Usuarios/Login.cshtml");
                }

                // ... Aquí va tu lógica actual para verificar la contraseña con Firebase ...
                // ... (por ejemplo: Firebase Auth, creación de la cookie de sesión, etc.) ...

                return RedirectToAction("Index", "Home"); // O a tu vista de inicio
            }
            // 3. Captura específica para fallos de conexión / Timeout de la base de datos
            catch (Exception ex) when (ex.InnerException is System.TimeoutException ||
                                      ex.Message.Contains("transient failure") ||
                                      ex.Message.Contains("Timeout"))
            {
                // Añadimos el mensaje amigable al ModelState
                ModelState.AddModelError(string.Empty, "No se pudo conectar con el servidor en este momento. Por favor, intente más tarde.");

                CargarConfiguracionFirebase(); // Recargamos configuración necesaria para la vista
                return View("~/Views/Usuarios/Login.cshtml");
            }
            // 4. Captura cualquier otro error inesperado
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado. Inténtelo de nuevo.");
                CargarConfiguracionFirebase();
                return View("~/Views/Usuarios/Login.cshtml");
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginInvitado()
        {
            var invitadoGuid = Guid.NewGuid().ToString();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"Invitado_{invitadoGuid.Substring(0, 5)}"),
                new Claim("Documento", "GUEST"),
                new Claim(ClaimTypes.Role, "Guest")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            return Ok(new { success = true, message = "Acceso como invitado concedido." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerDatosUsuario(string identificador)
        {
            if (string.IsNullOrEmpty(identificador)) return BadRequest("El identificador no puede estar vacío.");

            var usuario = await _authService.BuscarUsuarioPorIdOEmailAsync(identificador);
            if (usuario == null) return NotFound("El usuario no pertenece a la institución.");

            return Ok(new
            {
                documento = usuario.Documento,
                correo = usuario.Correo,
                necesitaVinculacion = string.IsNullOrEmpty(usuario.FirebaseUid)
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarSesionSrv(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return BadRequest("Identificador vacío");

            var usuario = await _authService.BuscarUsuarioPorIdOEmailAsync(documento);
            if (usuario == null) return BadRequest("Usuario no encontrado o inválido.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombres),
                new Claim("Documento", usuario.Documento.Trim()),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.NameIdentifier, usuario.FirebaseUid ?? ""),
                new Claim(ClaimTypes.Role, usuario.IdTipoUsuarioNavigation?.Descripcion ?? "Aprendiz")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            return Ok();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CompletarRegistro(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return RedirectToAction("Login");

            var usuario = await _authService.ObtenerPorDocumentoAsync(documento);
            if (usuario == null) return RedirectToAction("Login");

            CargarConfiguracionFirebase();
            ViewBag.Correo = usuario.Correo.Trim();

            return View("~/Views/Usuarios/CompletarRegistro.cshtml", model: usuario.Documento.Trim());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VincularPrimerIngreso([FromForm] string documento)
        {
            if (string.IsNullOrEmpty(documento)) return BadRequest("El documento es requerido.");

            try
            {
                await _authService.VincularPrimerIngresoAsync(documento);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Perfil()
        {
            if (User.IsInRole("Guest"))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            var documentoCookie = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(documentoCookie))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            var usuario = await _authService.GetByDocumentoAsync(documentoCookie.Trim());
            if (usuario == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth");
            }

            return View("~/Views/Usuarios/Perfil.cshtml", usuario);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult OlvidePassword()
        {
            return View("~/Views/Usuarios/OlvidePassword.cshtml");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> EnviarCodigo(string correo)
        {
            if (string.IsNullOrEmpty(correo))
            {
                ViewBag.Error = "El correo electrónico es obligatorio.";
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }

            var limpio = correo.Trim();

            try
            {
                await _authService.EnviarCodigoRecuperacionAsync(limpio);

                return RedirectToAction("VerificarCodigo", new { email = limpio });
            }
            catch (KeyNotFoundException ex)
            {
                ViewBag.Error = ex.Message;
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "No se pudo enviar el correo de recuperación. Inténtalo más tarde.";
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerificarCodigo(string email)
        {
            return View("~/Views/Usuarios/VerificarCodigo.cshtml", email);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarCodigo(string email, string codigo)
        {
            bool esValido = await _authService.ValidarCodigoRecuperacionAsync(email, codigo);
            if (!esValido)
            {
                ViewBag.Error = "El código es incorrecto o ya expiró.";
                return View("VerificarCodigo", model: email);
            }

            return RedirectToAction("NuevaPassword", new { email = email });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult NuevaPassword(string email)
        {
            return View("~/Views/Usuarios/NuevaPassword.cshtml", email);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ActualizarPassword(string email, string password, string confirmarPassword)
        {
            if (password != confirmarPassword)
            {
                ViewBag.Error = "Las contraseñas no coinciden.";
                return View("NuevaPassword", model: email);
            }

            try
            {
                await _authService.ActualizarPasswordAsync(email, password);
                TempData["Mensaje"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("NuevaPassword", model: email);
            }
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return RedirectToAction("Login", "Auth");
        }
    }
}