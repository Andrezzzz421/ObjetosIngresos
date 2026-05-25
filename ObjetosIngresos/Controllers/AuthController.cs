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
    public class AuthController:Controller
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

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Perfil");
            }

            return View("~/Views/Usuarios/Login.cshtml");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarSesionSrv(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return BadRequest("Documento vacío");

            var usuario = db.Usuarios.FirstOrDefault(u => u.Documento.Trim() == documento.Trim());

            if (usuario == null) return BadRequest("Usuario no encontrado");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombres),
                new Claim("Documento", usuario.Documento.Trim()),
                new Claim(ClaimTypes.NameIdentifier, usuario.FirebaseUid ?? "")
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

        [AllowAnonymous]
        public ActionResult CompletarRegistro(String documento)
        {
            ViewBag.Documento = documento;
            return View("~/Views/Usuarios/CompletarRegistro.cshtml");
        }


        [Authorize] 
        public async Task<IActionResult> Perfil()
        {
            var documentoCookie = User.FindFirst("Documento")?.Value;

            if (string.IsNullOrEmpty(documentoCookie))
            {
                return RedirectToAction("Login");
            }
              
            var usuario = await db.Usuarios
                .Include(u => u.IdSedePrincipalNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento.Trim() == documentoCookie.Trim());

            if (usuario == null) return RedirectToAction("Login");

            return View("~/Views/Usuarios/Perfil.cshtml", usuario);
        }
        [HttpPost]


        [HttpPost]
        [AllowAnonymous] 
        public async Task<IActionResult> VincularPrimerIngreso(string documento)
        {
            if (string.IsNullOrEmpty(documento))
            {
                return BadRequest("El número de documento es obligatorio.");
            }

            var documentoLimpio = documento.Trim();
            var usuarioLocal = await db.Usuarios
                .FirstOrDefaultAsync(u => u.Documento.Trim() == documentoLimpio);

            if (usuarioLocal == null)
            {
                return NotFound("El documento no existe en la base de datos de la institución.");
            }

            if (!string.IsNullOrEmpty(usuarioLocal.FirebaseUid))
            {
                return BadRequest("Este usuario ya ha sido vinculado anteriormente. Por favor, inicia sesión normalmente.");
            }

            try
            {
                string emailSintetico = $"{documentoLimpio}@sistema.com";

                string firebaseUid = await ser.RegistrarEnFirebase(emailSintetico, documentoLimpio);

                usuarioLocal.FirebaseUid = firebaseUid;
                await db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Vinculación exitosa",
                    uid = firebaseUid
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error técnico durante la vinculación: {ex.Message}");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutServidor()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            return Ok();
        }



        [HttpGet]
        public IActionResult OlvidePassword()
        { return View("~/Views/Usuarios/OlvidePassword.cshtml"); }




        [HttpPost]
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
                ViewBag.Error = "Error en Firebase: " + ex;
                return View("NuevaPassword", model: email);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error inesperado: " + ex.Message;
                return View("NuevaPassword", model: email);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarCodigo(string correo)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

            if (usuario != null)
            {
                string codigoGenerado = new Random().Next(100000, 999999).ToString();

                usuario.codigo_recuperacion = codigoGenerado;
                usuario.codigo_expiracion = DateTime.Now.AddMinutes(15);
                await db.SaveChangesAsync();

                try
                {
                    using (var client = new SmtpClient(config["Mailtrap:Host"], int.Parse(config["Mailtrap:Port"])))
                    {
                        client.Credentials = new NetworkCredential(config["Mailtrap:User"], config["Mailtrap:Pass"]);
                        client.EnableSsl = true;

                        var asunto = "Tu Código de Seguridad";
                        var cuerpo = $"Hola {usuario.Nombres}, tu código es: {codigoGenerado}";

                        client.Send("soporte@tuapp.com", correo, asunto, cuerpo);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Error al enviar correo: " + ex.Message;
                    return View("~/Views/Usuarios/OlvidePassword.cshtml");
                }

                return RedirectToAction("VerificarCodigo", new { email = correo });
            }

            ViewBag.Error = "El correo no está registrado.";
            return View("~/Views/Usuarios/OlvidePassword.cshtml");
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CambiarPasswordLogueado()
        { 
            var documento = User.FindFirst("Documento")?.Value;

            if (string.IsNullOrEmpty(documento)) return RedirectToAction("Login");
             
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento.Trim() == documento.Trim());

            if (usuario == null) return RedirectToAction("Login");
             
            return View("~/Views/Usuarios/NuevaPassword.cshtml", usuario.Correo);
        }
    }
}
