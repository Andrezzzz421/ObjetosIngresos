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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CompletarRegistro(string documento)
        {
            if (string.IsNullOrEmpty(documento))
            {
                return RedirectToAction("Login");
            }

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento.Trim() == documento.Trim());

            if (usuario == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.Correo = usuario.Correo;

            return View("~/Views/Usuarios/CompletarRegistro.cshtml", model: usuario.Correo);
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Perfil");
            ViewBag.FirebaseConfig = new{
                apiKey = config["FirebaseConfig:ApiKey"],
                authDomain = config["FirebaseConfig:AuthDomain"],
                projectId = config["FirebaseConfig:ProjectId"],
                storageBucket = config["FirebaseConfig:StorageBucket"],
                messagingSenderId = config["FirebaseConfig:MessagingSenderId"],
                appId = config["FirebaseConfig:AppId"]
            };
            return View("~/Views/Usuarios/Login.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerDatosUsuario(string identificador)
        {
            var usuario = await db.Usuarios
                .FirstOrDefaultAsync(u => u.Documento == identificador || u.Correo == identificador);

            if (usuario == null) return NotFound("Usuario no registrado.");

            return Ok(new
            {
                correo = usuario.Correo,
                necesitaVinculacion = string.IsNullOrEmpty(usuario.FirebaseUid)
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarSesionSrv(string identificador)
        {
            if (string.IsNullOrEmpty(identificador)) return BadRequest("Identificador vacío");

            var usuario = await db.Usuarios
                .FirstOrDefaultAsync(u => u.Documento.Trim() == identificador.Trim() || u.Correo.Trim() == identificador.Trim());

            if (usuario == null) return BadRequest("Usuario no encontrado");

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nombres),
            new Claim("Documento", usuario.Documento.Trim()),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.NameIdentifier, usuario.FirebaseUid ?? "")
        };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) });

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> VincularPrimerIngreso([FromForm] string documento)
        {
            if (string.IsNullOrEmpty(documento))
            {
                return BadRequest("El documento es requerido.");
            }

            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento.Trim() == documento.Trim());

            if (usuario == null)
            {
                return BadRequest("El usuario no pertenece a la institución.");
            }

            if (!string.IsNullOrEmpty(usuario.FirebaseUid))
            {
                return BadRequest("Este usuario ya se encuentra vinculado.");
            }

            try
            {
                var args = new UserRecordArgs
                {
                    Email = usuario.Correo,
                    Password = documento.Trim(), 
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


        [Authorize]
        public async Task<IActionResult> Perfil()
        {
            var documentoCookie = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(documentoCookie)) return RedirectToAction("Login");

            var usuario = await db.Usuarios
                .Include(u => u.IdSedePrincipalNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == documentoCookie);

            return View("~/Views/Usuarios/Perfil.cshtml", usuario);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarCodigo(string correo)
        {
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
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
                using var client = new SmtpClient(config["Mailtrap:Host"], int.Parse(config["Mailtrap:Port"]));
                client.Credentials = new NetworkCredential(config["Mailtrap:User"], config["Mailtrap:Pass"]);
                client.EnableSsl = true;
                client.Send("soporte@tuapp.com", correo, "Código de Recuperación", $"Tu código es: {codigoGenerado}");

                return RedirectToAction("VerificarCodigo", new { email = correo });
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error al enviar correo: " + ex.Message;
                return View("~/Views/Usuarios/OlvidePassword.cshtml");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LogoutServidor()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok();
        }


        [HttpGet]
        public IActionResult OlvidePassword()
        { return View("~/Views/Usuarios/OlvidePassword.cshtml"); }



        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ActualizarPassword([FromForm] string email, [FromForm] string password, [FromForm] string confirmarPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return BadRequest("El correo y la contraseña son obligatorios.");
            }

            if (password != confirmarPassword)
            {
                return BadRequest("Las contraseñas no coinciden.");
            }

            var usuarioLocal = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == email.Trim());

            if (usuarioLocal == null || string.IsNullOrEmpty(usuarioLocal.FirebaseUid))
            {
                return BadRequest("No se encontró el registro de autenticación para este usuario.");
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

                return Ok(new { success = true, message = "Contraseña actualizada con éxito." });
            }
            catch (FirebaseAuthException ex)
            {
                return BadRequest($"Error en el proveedor de identidad: {ex}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ocurrió un error inesperado: {ex.Message}");
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
    }
}
