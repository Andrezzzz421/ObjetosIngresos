using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using System.Security.Claims;

namespace ObjetosIngresos.Controllers
{
    [Authorize]
    public class NotificacionesController : Controller
    {
        private readonly SistemaIngresoContext _db;

        public NotificacionesController(SistemaIngresoContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Devuelve las últimas 20 notificaciones del usuario en sesión.
        /// Llamado por el JavaScript del layout cada vez que se abre la campana.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerRecientes()
        {
            var doc = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(doc))
                return Json(new { success = false });

            var idUsuario = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Documento == doc.Trim())
                .Select(u => (int?)u.IdUsuario)
                .FirstOrDefaultAsync();

            if (idUsuario == null)
                return Json(new { success = false });

            try
            {
                var notifs = await _db.Notificaciones
                    .AsNoTracking()
                    .Where(n => n.IdUsuario == idUsuario.Value)
                    .OrderByDescending(n => n.FechaCreacion)
                    .Take(20)
                    .Select(n => new
                    {
                        id      = n.IdNotificacion,
                        tipo    = n.Tipo,
                        titulo  = n.Titulo,
                        mensaje = n.Mensaje,
                        leida   = n.Leida,
                        fecha   = n.FechaCreacion.AddHours(-5).ToString("dd/MM/yyyy HH:mm")
                    })
                    .ToListAsync();

                return Json(new { success = true, notificaciones = notifs });
            }
            catch
            {
                // La tabla puede no existir aún en la BD (pendiente de migración).
                return Json(new { success = true, notificaciones = Array.Empty<object>() });
            }
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            var doc = User.FindFirst("Documento")?.Value;
            if (string.IsNullOrEmpty(doc))
                return Json(new { success = false });

            var idUsuario = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Documento == doc.Trim())
                .Select(u => (int?)u.IdUsuario)
                .FirstOrDefaultAsync();

            if (idUsuario == null)
                return Json(new { success = false });

            try
            {
                await _db.Notificaciones
                    .Where(n => n.IdUsuario == idUsuario.Value && !n.Leida)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
            }
            catch { /* Tabla no existe aún */ }

            return Json(new { success = true });
        }

        /// <summary>
        /// Crea una notificación para un usuario dado.
        /// Llamado desde MovimientoServices al registrar entrada/salida.
        /// </summary>
        public static async Task CrearAsync(SistemaIngresoContext db, int idUsuario,
            string tipo, string titulo, string mensaje)
        {
            try
            {
                db.Notificaciones.Add(new Notificacion
                {
                    IdUsuario      = idUsuario,
                    Tipo           = tipo,
                    Titulo         = titulo,
                    Mensaje        = mensaje,
                    Leida          = false,
                    FechaCreacion  = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch { /* No bloquear el flujo si la tabla no existe */ }
        }
    }
}
