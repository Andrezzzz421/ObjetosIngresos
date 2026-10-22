using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using ObjetosIngresos.ViewModel;
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
            if (string.IsNullOrWhiteSpace(documento))
                throw new ArgumentException("El documento no puede estar vacío.");

            var limpio = documento.Trim();
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Documento == limpio)
                ?? throw new KeyNotFoundException("El usuario no pertenece a la institución.");

            if (!string.IsNullOrEmpty(usuario.FirebaseUid))
                throw new InvalidOperationException("Este usuario ya se encuentra vinculado.");

            string passwordInicial = limpio.Length < 6 ? limpio.PadRight(6, '0') : limpio;
            string emailLimpio = usuario.Correo.Trim().ToLower();

            string firebaseUid = string.Empty;

            try
            {
                var args = new UserRecordArgs
                {
                    Email = emailLimpio,
                    Password = passwordInicial,
                    DisplayName = $"{usuario.Nombres} {usuario.Apellidos}".Trim()
                };

                UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
                firebaseUid = userRecord.Uid;
            }
            catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
            {
                UserRecord userExistente = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(emailLimpio);
                firebaseUid = userExistente.Uid;

                var updateArgs = new UserRecordArgs
                {
                    Uid = firebaseUid,
                    Password = passwordInicial
                };
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error de comunicación con Firebase Auth: {ex.Message}");
            }

            try
            {
                usuario.FirebaseUid = firebaseUid;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error guardando el UID en la base de datos: {ex.InnerException?.Message ?? ex.Message}");
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

        public async Task EnviarCodigoPreRegistroAsync(string correo, string nombres, string codigoGenerado)
        {
            var limpio = correo.Trim();
            var host = _config["SmtpConfig:Host"];
            var portStr = _config["SmtpConfig:Port"];
            int port = int.TryParse(portStr, out int p) ? p : 587;
            var senderEmail = _config["SmtpConfig:SenderEmail"] ?? "no-reply@sgi.local";
            var senderName = _config["SmtpConfig:SenderName"] ?? "SGI - Sistema de Ingreso";
            var pass = _config["SmtpConfig:Pass"];

            var asunto = "Tu Código de Verificación de Pre-Registro - SGI";
            var cuerpoHtml = $@"
            <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; padding: 32px 16px; margin: 0 auto; max-width: 520px;'>
                <div style='background: #ffffff; border: 1px solid #e2e8f0; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>
                    <div style='background: linear-gradient(135deg, #4f46e5 0%, #3730a3 100%); padding: 28px 24px; text-align: center;'>
                        <h1 style='color: #ffffff; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.5px;'>Verificación de Pre-Registro</h1>
                        <p style='color: #c7d2fe; margin: 6px 0 0 0; font-size: 13px;'>Sistema de Gestión de Ingresos (SGI)</p>
                    </div>
                    <div style='padding: 28px 24px; text-align: center;'>
                        <p style='color: #1e293b; font-size: 16px; margin: 0 0 12px 0;'>Hola <strong>{nombres}</strong>,</p>
                        <p style='color: #64748b; font-size: 14px; line-height: 1.5; margin: 0 0 24px 0;'>
                            Has iniciado tu pre-registro en el sistema. Para confirmar que este correo te pertenece y activar tu cuenta, ingresa el siguiente código de verificación:
                        </p>
                        <div style='background-color: #eef2ff; border: 2px dashed #4f46e5; border-radius: 14px; padding: 18px; margin: 0 auto 24px auto; font-size: 34px; font-weight: 800; letter-spacing: 8px; color: #4f46e5; font-family: monospace;'>
                            {codigoGenerado}
                        </div>
                        <div style='background-color: #f1f5f9; border-radius: 12px; padding: 14px; text-align: left; margin-bottom: 20px;'>
                            <p style='color: #334155; font-size: 13px; font-weight: 600; margin: 0 0 4px 0;'>📌 Recuerda:</p>
                            <p style='color: #64748b; font-size: 12px; margin: 0; line-height: 1.4;'>
                                Una vez verificado, podrás ingresar con tu documento y registrar tus pertenencias o equipos personales cómodamente desde <strong>Mi Panel</strong>.
                            </p>
                        </div>
                        <p style='color: #94a3b8; font-size: 12px; margin: 0;'>
                            Este código vencerá en <strong>15 minutos</strong>. Si tú no solicitaste este registro, puedes ignorar este mensaje de forma segura.
                        </p>
                    </div>
                    <div style='background-color: #f8fafc; border-top: 1px solid #f1f5f9; padding: 16px; text-align: center;'>
                        <p style='color: #94a3b8; font-size: 11px; margin: 0;'>&copy; {DateTime.UtcNow.Year} - Sistema de Gestión de Ingresos</p>
                    </div>
                </div>
            </div>";

            using var client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(senderEmail, pass);
            client.EnableSsl = true;

            using var mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(senderEmail, senderName);
            mailMessage.To.Add(limpio);
            mailMessage.Subject = asunto;
            mailMessage.Body = cuerpoHtml;
            mailMessage.IsBodyHtml = true;
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

        public async Task<(int totalEquipos, int equiposEnSede, int totalMovimientos, string? ultimoIngreso)> ObtenerEstadisticasUsuarioAsync(int idUsuario)
        {
            var equipos = await _db.Elementos
                .AsNoTracking()
                .Where(e => e.IdUsuario == idUsuario)
                .Select(e => e.IdElemento)
                .ToListAsync();

            int totalEquipos = equipos.Count;
            int equiposEnSede = 0;
            int totalMovimientos = 0;
            string? ultimoIngreso = null;

            if (equipos.Any())
            {
                var movs = await _db.RegistrosMovimientos
                    .AsNoTracking()
                    .Where(m => equipos.Contains(m.IdElemento))
                    .OrderByDescending(m => m.FechaEntrada)
                    .Select(m => new { m.FechaEntrada, m.FechaSalida })
                    .ToListAsync();

                totalMovimientos = movs.Count;
                equiposEnSede = movs.Count(m => m.FechaSalida == null);

                var ult = movs.FirstOrDefault(m => m.FechaEntrada != null);
                if (ult?.FechaEntrada != null)
                {
                    ultimoIngreso = ult.FechaEntrada.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt");
                }
            }

            return (totalEquipos, equiposEnSede, totalMovimientos, ultimoIngreso);
        }

        public async Task<PerfilViewModel?> ObtenerPerfilViewModelAsync(string documentoCookie)
        {
            if (string.IsNullOrWhiteSpace(documentoCookie)) return null;

            var limpio = documentoCookie.Trim();
            var usuario = await _db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .FirstOrDefaultAsync(u => u.Documento == limpio);

            if (usuario == null) return null;

            var culture = new System.Globalization.CultureInfo("es-ES");
            var hoy = DateTime.Now;

            var vm = new PerfilViewModel
            {
                Usuario = usuario,
                NombreCompleto = $"{usuario.Nombres} {usuario.Apellidos}".Trim(),
                TipoUsuarioDescripcion = usuario.IdTipoUsuarioNavigation?.Descripcion ?? "Usuario",
                CodigoUsuario = $"USR-{usuario.IdUsuario:D5}",
                Ficha = usuario.Ficha,
                SedeNombre = usuario.IdSedePrincipalNavigation?.NombreSede ?? "Sin sede asignada",
                SedeCiudad = usuario.IdSedePrincipalNavigation?.Ciudad ?? ""
            };

            var equiposDb = await _db.Elementos
                .AsNoTracking()
                .Include(e => e.IdMarcaNavigation)
                .Include(e => e.RegistrosMovimientos)
                .Where(e => e.IdUsuario == usuario.IdUsuario)
                .ToListAsync();

            var idsEquipos = equiposDb.Select(e => e.IdElemento).ToList();

            var movsDb = await _db.RegistrosMovimientos
                .AsNoTracking()
                .Include(m => m.IdElementoNavigation)
                    .ThenInclude(el => el.IdMarcaNavigation)
                .Include(m => m.IdSedeNavigation)
                .Where(m => idsEquipos.Contains(m.IdElemento))
                .OrderByDescending(m => m.FechaEntrada)
                .ToListAsync();

            vm.TotalEquipos = equiposDb.Count;
            vm.EquiposEnSede = equiposDb.Count(e => e.RegistrosMovimientos.Any(m => m.FechaSalida == null));
            vm.EquiposFueraDeSede = Math.Max(0, vm.TotalEquipos - vm.EquiposEnSede);
            vm.TotalMovimientos = movsDb.Count;

            var ultIngreso = movsDb.FirstOrDefault(m => m.FechaEntrada != null);
            if (ultIngreso?.FechaEntrada != null)
            {
                var f = ultIngreso.FechaEntrada.Value.AddHours(-5);
                string sedeIngreso = ultIngreso.IdSedeNavigation?.NombreSede ?? "Portería principal";
                string horaStr = f.ToString("HH:mm");

                if (f.Date == hoy.Date)
                {
                    vm.UltimoIngresoHora = $"Hoy, {horaStr}";
                }
                else if (f.Date == hoy.AddDays(-1).Date)
                {
                    vm.UltimoIngresoHora = $"Ayer, {horaStr}";
                }
                else
                {
                    vm.UltimoIngresoHora = $"{f:dd MMM yyyy}, {horaStr}";
                }

                vm.UltimoIngresoDetalle = $"{f:dd MMM yyyy} · {sedeIngreso}";
                vm.EstaEnSede = vm.EquiposEnSede > 0;
                vm.EstadoEnSedeTexto = vm.EstaEnSede ? $"En sede desde las {horaStr}" : "Fuera de sede";
            }
            else
            {
                vm.UltimoIngresoHora = "Sin registros";
                vm.UltimoIngresoDetalle = "No hay movimientos registrados";
                vm.EstaEnSede = false;
                vm.EstadoEnSedeTexto = "Fuera de sede";
            }

            foreach (var eq in equiposDb)
            {
                string tipoLower = (eq.TipoElemento ?? "").ToLower();
                string icono = "laptop";
                string tipoNombre = eq.TipoElemento ?? "Equipo";

                if (tipoLower.Contains("portatil") || tipoLower.Contains("laptop") || tipoLower.Contains("pc") || tipoLower.Contains("computador"))
                {
                    icono = "laptop";
                    tipoNombre = "Portátil";
                }
                else if (tipoLower.Contains("mouse") || tipoLower.Contains("raton"))
                {
                    icono = "mouse";
                    tipoNombre = "Mouse inalámbrico";
                }
                else if (tipoLower.Contains("camara") || tipoLower.Contains("camera"))
                {
                    icono = "camera";
                    tipoNombre = "Cámara digital";
                }
                else if (tipoLower.Contains("tablet") || tipoLower.Contains("tableta") || tipoLower.Contains("wacom") || tipoLower.Contains("ipad"))
                {
                    icono = "tablet";
                    tipoNombre = "Tableta gráfica";
                }

                string nombreMarca = eq.IdMarcaNavigation?.NombreMarca ?? "";
                string nombreMostrar = !string.IsNullOrWhiteSpace(nombreMarca)
                    ? $"{nombreMarca} {eq.TipoElemento}"
                    : (eq.TipoElemento ?? "Equipo");

                vm.Equipos.Add(new PerfilEquipoItem
                {
                    IdElemento = eq.IdElemento,
                    Codigo = $"EQ-{eq.IdElemento:D5}",
                    Nombre = nombreMostrar,
                    Tipo = tipoNombre,
                    Marca = !string.IsNullOrWhiteSpace(nombreMarca) ? nombreMarca : "Sin marca",
                    Serial = !string.IsNullOrWhiteSpace(eq.Serial) ? eq.Serial : "Sin serial",
                    EnSede = eq.RegistrosMovimientos.Any(m => m.FechaSalida == null),
                    IconoTipo = icono
                });
            }

            foreach (var mov in movsDb.Take(10))
            {
                bool esIngreso = mov.FechaSalida == null;
                var fechaRef = (esIngreso ? mov.FechaEntrada : (mov.FechaSalida ?? mov.FechaEntrada))?.AddHours(-5) ?? DateTime.Now;

                string fechaTxt = fechaRef.Date == hoy.Date ? "Hoy · " + fechaRef.ToString("dd MMM", culture) :
                                 fechaRef.Date == hoy.AddDays(-1).Date ? "Ayer · " + fechaRef.ToString("dd MMM", culture) :
                                 fechaRef.ToString("dd MMM yyyy", culture);

                string eqNombre = mov.IdElementoNavigation != null
                    ? (!string.IsNullOrWhiteSpace(mov.IdElementoNavigation.IdMarcaNavigation?.NombreMarca) 
                        ? $"{mov.IdElementoNavigation.IdMarcaNavigation.NombreMarca} {mov.IdElementoNavigation.TipoElemento}" 
                        : mov.IdElementoNavigation.TipoElemento)
                    : "Equipo";

                vm.MovimientosRecientes.Add(new PerfilMovimientoItem
                {
                    IdMovimiento = mov.IdMovimiento,
                    EsIngreso = esIngreso,
                    Titulo = esIngreso ? "Ingreso de 1 equipo" : "Salida de 1 equipo",
                    Ubicacion = mov.IdSedeNavigation?.NombreSede ?? "Portería principal",
                    DescripcionEquipos = eqNombre,
                    Hora = fechaRef.ToString("HH:mm"),
                    Fecha = fechaTxt
                });
            }

            return vm;
        }
    }
}