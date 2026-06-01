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

        private async Task<Usuario?> BuscarUsuarioPorIdOEmailAsync(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return null;

            var limpio = identificador.Trim();
            return await db.Usuarios
                .Include(u=>u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == limpio || u.Correo == limpio);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> CompletarRegistro(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return RedirectToAction("Login");

            var limpio = documento.Trim();
            var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio);

            if (usuario == null) return RedirectToAction("Login");

            ViewBag.Correo = usuario.Correo;
            return View("~/Views/Usuarios/CompletarRegistro.cshtml", model: usuario.Correo);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Guest"))
            {
                return RedirectToAction("Perfil");
            }

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
            var usuario = await BuscarUsuarioPorIdOEmailAsync(identificador);
            if (usuario == null) return NotFound("Usuario no registrado.");

            return Ok(new
            {
                correo = usuario.Correo.Trim(),
                documento = usuario.Documento.Trim(),
                necesitaVinculacion = string.IsNullOrEmpty(usuario.FirebaseUid)
            });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> GenerarSesionSrv(string identificador)
        {
            var usuario = await BuscarUsuarioPorIdOEmailAsync(identificador);
            if (usuario == null) return BadRequest("Usuario no encontrado o inválido.");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombres),
                new Claim("Documento", usuario.Documento.Trim()),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.NameIdentifier, usuario.FirebaseUid ?? ""),
                new Claim(ClaimTypes.Role,usuario.IdTipoUsuarioNavigation?.Descripcion ?? "Aprendiz")
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
                return BadRequest($"Error al crear el registro inicial: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(Roles ="Aprendiz,Instructor,Administrador")]
        [Authorize]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Perfil()
        {
            var documentoCookie = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(documentoCookie)) return RedirectToAction("Login","Auth");

            var usuario = await db.Usuarios
                .Include(u => u.IdSedePrincipalNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == documentoCookie);

            return View("~/Views/Usuarios/Perfil.cshtml", usuario);
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
                using var client = new SmtpClient(config["Mailtrap:Host"], int.Parse(config["Mailtrap:Port"]))
                {
                    Credentials = new NetworkCredential(config["Mailtrap:User"], config["Mailtrap:Pass"]),
                    EnableSsl = true
                };

                client.Send("soporte@tuapp.com", limpio, "Código de Recuperación", $"Tu código es: {codigoGenerado}");

                return RedirectToAction("VerificarCodigo", new { email = limpio });
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
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> CerrarSesionSrv()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);


            return RedirectToAction("Login", "Auth");
        }
    }
}
