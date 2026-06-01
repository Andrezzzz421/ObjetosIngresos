using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    public class AuthController : Controller
    {
        private readonly SistemaIngresoContext db;
        private readonly AuthServices ser;
        private readonly IConfiguration config;

        public AuthController(SistemaIngresoContext db, AuthServices authService, IConfiguration config)
        {
            this.db = db;
            this.ser = authService;
            this.config = config;
        }

        private async Task<Usuario?> BuscarUsuarioPorIdOEmailAsync(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return null;

            var limpio = identificador.Trim();
            return await db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == limpio || u.Correo == limpio);
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

            ViewBag.FirebaseConfig = new
            {
                apiKey = config["FirebaseConfig:ApiKey"],
                authDomain = config["FirebaseConfig:AuthDomain"],
                projectId = config["FirebaseConfig:ProjectId"],
                storageBucket = config["FirebaseConfig:StorageBucket"],
                messagingSenderId = config["FirebaseConfig:MessagingSenderId"],
                appId = config["FirebaseConfig:AppId"]
            };

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

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok(new { success = true, message = "Acceso como invitado concedido." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerDatosUsuario(string identificador)
        {
            if (string.IsNullOrEmpty(identificador))
            {
                return BadRequest("El identificador no puede estar vacío.");
            }

            var usuario = await db.Usuarios
                .FirstOrDefaultAsync(u => u.Documento == identificador || u.Correo == identificador);

            if (usuario == null)
            {
                return NotFound("El usuario no pertenece a la institución.");
            }

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

            var usuario = await BuscarUsuarioPorIdOEmailAsync(documento);
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

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return Ok();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CompletarRegistro(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return RedirectToAction("Login");

            var limpio = documento.Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio);

            if (usuario == null) return RedirectToAction("Login");

            ViewBag.FirebaseConfig = new
            {
                apiKey = config["FirebaseConfig:ApiKey"],
                authDomain = config["FirebaseConfig:AuthDomain"],
                projectId = config["FirebaseConfig:ProjectId"],
                storageBucket = config["FirebaseConfig:StorageBucket"],
                messagingSenderId = config["FirebaseConfig:MessagingSenderId"],
                appId = config["FirebaseConfig:AppId"]
            };

            ViewBag.Correo = usuario.Correo.Trim();

            return View("~/Views/Usuarios/CompletarRegistro.cshtml", model: usuario.Documento.Trim());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VincularPrimerIngreso([FromForm] string documento)
        {
            if (string.IsNullOrEmpty(documento)) return BadRequest("El documento es requerido.");

            var limpio = documento.Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio);

            if (usuario == null) return BadRequest("El usuario no pertenece a la institución.");
            if (!string.IsNullOrEmpty(usuario.FirebaseUid)) return BadRequest("Este usuario ya se encuentra vinculado.");

            try
            {
                var args = new UserRecordArgs
                {
                    Email = usuario.Correo.Trim(),
                    Password = limpio,
                    DisplayName = $"{usuario.Nombres} {usuario.Apellidos}"
                };

                UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
                usuario.FirebaseUid = userRecord.Uid;
                await db.SaveChangesAsync();

                return Ok();
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
            {
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(usuario.Correo);
                usuario.FirebaseUid = userRecord.Uid;
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al crear el usuario en Firebase: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Aprendiz,Instructor,Administrador")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Perfil()
        {
            var documentoCookie = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(documentoCookie)) return RedirectToAction("Login", "Auth");

            var usuario = await db.Usuarios
                .Include(u => u.IdSedePrincipalNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == documentoCookie);

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

            var limpio = correo.Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == limpio);

            if (usuario == null)
            {
                ViewBag.Error = "El correo no está registrado.";
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }

            string codigoGenerado = new Random().Next(100000, 999999).ToString();
            usuario.codigo_recuperacion = codigoGenerado;
            usuario.codigo_expiracion = DateTime.Now.AddMinutes(15);
            await db.SaveChangesAsync();

            try
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

                    var asunto = "Tu Código de Seguridad";
                    var cuerpo = $"Hola {usuario.Nombres}, tu código es: {codigoGenerado}";

                    // Se usa MailMessage para asegurar el remitente real y evitar bloqueos de Google
                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail, senderName);
                        mailMessage.To.Add(limpio);
                        mailMessage.Subject = asunto;
                        mailMessage.Body = cuerpo;
                        mailMessage.IsBodyHtml = false; // Cambiar a true si después le meten una plantilla HTML
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
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == email && u.codigo_recuperacion == codigo);

            if (usuario == null || usuario.codigo_expiracion < DateTime.Now)
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

            var usuarioLocal = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == email);

            if (usuarioLocal == null || string.IsNullOrEmpty(usuarioLocal.FirebaseUid))
            {
                ViewBag.Error = "No se encontró el registro de autenticación para este usuario.";
                return View("NuevaPassword", model: email);
            }

            try
            {
                var args = new UserRecordArgs
                {
                    Uid = usuarioLocal.FirebaseUid,
                    Password = password
                };

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

                usuarioLocal.codigo_recuperacion = null;
                usuarioLocal.codigo_expiracion = null;

                await db.SaveChangesAsync();

                TempData["Mensaje"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
                return RedirectToAction("Login");
            }
            catch (FirebaseAuthException ex)
            {
                ViewBag.Error = "Error en Firebase: " + ex.Message;
                return View("NuevaPassword", model: email);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error inesperado: " + ex.Message;
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