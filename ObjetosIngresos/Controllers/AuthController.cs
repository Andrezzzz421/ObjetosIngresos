using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ObjetosIngresos.Services;
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

            CargarConfiguracionFirebase();
            return View("~/Views/Usuarios/Login.cshtml");
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
        [Authorize(Roles = "Aprendiz,Instructor,Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Perfil()
        {
            var documentoCookie = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(documentoCookie)) return RedirectToAction("Login", "Auth");

            var usuario = await _authService.ObtenerPerfilCompletoAsync(documentoCookie);
            if (usuario == null) return RedirectToAction("Login", "Auth");

            return View("~/Views/Usuarios/Perfil.cshtml", usuario);
        }

        [HttpGet]
        public IActionResult OlvidePassword()
        {
            return View("~/Views/Usuarios/OlvidePassword.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> EnviarCodigo(string correo)
        {
            if (string.IsNullOrEmpty(correo)) return BadRequest("El correo es obligatorio.");

            try
            {
                await _authService.EnviarCodigoRecuperacionAsync(correo);
                return RedirectToAction("VerificarCodigo", new { email = correo.Trim() });
            }
            catch (KeyNotFoundException ex)
            {
                var host = config["SmtpConfig:Host"];
                var port = int.Parse(config["SmtpConfig:Port"]);
                var senderEmail = config["SmtpConfig:SenderEmail"];
                var senderName = config["SmtpConfig:SenderName"];
                var pass = config["SmtpConfig:Pass"];

                using (var client = new SmtpClient(host, port))
                {
                    client.Credentials = new NetworkCredential(senderEmail, pass);
                    client.EnableSsl = true;

                    var asunto = "Tu Código de Seguridad - Sistema de Ingreso";

                    // Aquí armamos el diseño HTML bien bacano
                    var cuerpoHtml = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f3f4f6; padding: 30px; border-radius: 16px; max-width: 500px; margin: 0 auto; text-align: center; border: 1px solid #e5e7eb;'>
                        <div style='background-color: #4f46e5; padding: 20px; border-radius: 12px 12px 0 0; margin: -30px -30px 20px -30px;'>
                            <h2 style='color: white; margin: 0; font-size: 24px;'>Verificación de Seguridad</h2>
                        </div>
                        <p style='color: #374151; font-size: 16px; line-height: 1.5;'>Hola <strong>{usuario.Nombres}</strong>,</p>
                        <p style='color: #6b7280; font-size: 14px;'>Has solicitado un código para recuperar tu cuenta. Usa el siguiente token de seguridad:</p>
                        
                        <div style='background-color: #eef2ff; border: 2px dashed #4f46e5; border-radius: 12px; padding: 15px; margin: 25px 0; font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4f46e5;'>
                            {codigoGenerado}
                        </div>
                        
                        <p style='color: #9ca3af; font-size: 12px; margin-top: 25px;'>Este código expirará en 15 minutos. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                        <hr style='border: 0; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                        <p style='color: #6b7280; font-size: 12px; margin: 0;'>&copy; {DateTime.Now.Year} - Sistema de Ingreso Elementos</p>
                    </div>";

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(limpio);
                        mailMessage.Subject = asunto;
                        mailMessage.Body = cuerpoHtml;
                        mailMessage.IsBodyHtml = true; // <-- AQUÍ SE ACTIVA LA MAGIA DEL HTML
                        mailMessage.BodyEncoding = System.Text.Encoding.UTF8;

                        client.Send(mailMessage);
                    }
                }
                return RedirectToAction("VerificarCodigo", new { email = limpio });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al enviar correo: " + ex.Message;
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }
        }

        [HttpGet]
        public IActionResult VerificarCodigo(string email)
        {
            return View("~/Views/Usuarios/VerificarCodigo.cshtml", email);
        }

        [HttpPost]
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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> CerrarSesionSrv()
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