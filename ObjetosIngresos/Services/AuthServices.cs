using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using System.Net;
using System.Net.Mail;

namespace ObjetosIngresos.Services
{
    public class AuthServices
    {
        private readonly SistemaIngresoContext _db;
        private readonly IConfiguration _config;

        public AuthServices(SistemaIngresoContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<Usuario?> BuscarUsuarioPorIdOEmailAsync(string identificador)
        {
            if (string.IsNullOrWhiteSpace(identificador)) return null;

            var limpio = identificador.Trim();
            return await _db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == limpio || u.Correo == limpio);
        }

        public async Task<Usuario?> ObtenerPorDocumentoAsync(string documento)
        {
            if (string.IsNullOrEmpty(documento)) return null;
            var limpio = documento.Trim();
            return await _db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio);
        }

        public async Task<Usuario?> ObtenerPerfilCompletoAsync(string documentoCookie)
        {
            return await _db.Usuarios
                .Include(u => u.IdSedePrincipalNavigation)
                .Include(u => u.IdTipoUsuarioNavigation)
                .FirstOrDefaultAsync(u => u.Documento == documentoCookie);
        }

        public async Task VincularPrimerIngresoAsync(string documento)
        {
            var limpio = documento.Trim();
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio)
                ?? throw new Exception("El usuario no pertenece a la institución.");

            if (!string.IsNullOrEmpty(usuario.FirebaseUid))
                throw new Exception("Este usuario ya se encuentra vinculado.");

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
                await _db.SaveChangesAsync();
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
            {
                var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(usuario.Correo);
                usuario.FirebaseUid = userRecord.Uid;
                await _db.SaveChangesAsync();
            }
        }

        public async Task EnviarCodigoRecuperacionAsync(string correo)
        {
            var limpio = correo.Trim();
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == limpio)
                ?? throw new KeyNotFoundException("El correo no está registrado.");

            string codigoGenerado = new Random().Next(100000, 999999).ToString();
            usuario.codigo_recuperacion = codigoGenerado;
            usuario.codigo_expiracion = DateTime.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync();

            var host = _config["SmtpConfig:Host"];
            var port = int.Parse(_config["SmtpConfig:Port"]);
            var senderEmail = _config["SmtpConfig:SenderEmail"];
            var senderName = _config["SmtpConfig:SenderName"];
            var pass = _config["SmtpConfig:Pass"];

            var asunto = "Tu Código de Seguridad - SGI";
            var cuerpoHtml = $@"
            <div style='font-family: Arial, sans-serif; background-color: #f3f4f6; padding: 30px; border-radius: 16px; max-width: 500px; margin: 0 auto; text-align: center; border: 1px solid #e5e7eb;'>
                <div style='background-color: #4f46e5; padding: 20px; border-radius: 12px 12px 0 0; margin: -30px -30px 20px -30px;'>
                    <h2 style='color: white; margin: 0; font-size: 24px;'>Verificación de Seguridad</h2>
                </div>
                <p style='color: #374151; font-size: 16px; line-height: 1.5;'>Hola <strong>{usuario.Nombres} {usuario.Apellidos}</strong>,</p>
                <p style='color: #6b7280; font-size: 14px;'>Has solicitado un código para recuperar tu cuenta en el SGI. Usa el siguiente token de seguridad:</p>
        
                <div style='background-color: #eef2ff; border: 2px dashed #4f46e5; border-radius: 12px; padding: 15px; margin: 25px 0; font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #4f46e5;'>
                    {codigoGenerado}
                </div>
        
                <p style='color: #9ca3af; font-size: 12px; margin-top: 25px;'>Este código expirará en 15 minutos. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
                <hr style='border: 0; border-top: 1px solid #e5e7eb; margin: 20px 0;'>
                <p style='color: #6b7280; font-size: 12px; margin: 0;'>&copy; {DateTime.UtcNow.Year} - Sistema de Gestión de Ingresos</p>
            </div>";

            using var client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(senderEmail, pass);
            client.EnableSsl = true;

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(senderEmail, senderName);
            mailMessage.To.Add(limpio);
            mailMessage.Subject = asunto;
            mailMessage.Body = cuerpoHtml;
            mailMessage.IsBodyHtml = true; // Activamos el renderizado HTML
            mailMessage.BodyEncoding = System.Text.Encoding.UTF8;

            await client.SendMailAsync(mailMessage);
        }
        

        public async Task<bool> ValidarCodigoRecuperacionAsync(string email, string codigo)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == email && u.codigo_recuperacion == codigo);
            if (usuario == null || usuario.codigo_expiracion < DateTime.UtcNow)
            {
                return false;
            }
            return true;
        }

        public async Task ActualizarPasswordAsync(string email, string password)
        {
            var usuarioLocal = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == email)
                ?? throw new Exception("No se encontró el registro de autenticación para este usuario.");

            if (string.IsNullOrEmpty(usuarioLocal.FirebaseUid))
                throw new Exception("El usuario local no tiene un UID de Firebase asociado.");

            var args = new UserRecordArgs
            {
                Uid = usuarioLocal.FirebaseUid,
                Password = password
            };

            await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

            usuarioLocal.codigo_recuperacion = null;
            usuarioLocal.codigo_expiracion = null;
            await _db.SaveChangesAsync();
        }

        public async Task<Usuario?> GetByDocumentoAsync(string documento)
        {
            return await _db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .FirstOrDefaultAsync(u => u.Documento == documento);
        }
    }
}