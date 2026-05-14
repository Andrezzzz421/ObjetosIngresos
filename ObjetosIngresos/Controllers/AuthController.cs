using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.Services;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    public class AuthController:Controller
    {
        private readonly SistemaIngresoContext db;
        private readonly AuthServices ser;

        public AuthController(SistemaIngresoContext db, AuthServices authService)
        {
            this.db = db;
            this.ser = authService;
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

    }
}
