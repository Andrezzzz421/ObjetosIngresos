using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class MovimientoServices
    {
        private readonly SistemaIngresoContext _db;

        public MovimientoServices(SistemaIngresoContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Busca elementos por documento del propietario o número de serie del equipo.
        /// </summary>
        public async Task<List<Elemento>> BuscarElementosAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await _db.Elementos
                    .Include(e => e.IdMarcaNavigation)
                    .Include(e => e.IdUsuarioNavigation)
                    .Include(e => e.RegistrosMovimientos)
                    .OrderByDescending(e => e.IdElemento)
                    .Take(100)
                    .AsNoTracking()
                    .ToListAsync();
            }

            var q = query.Trim().ToLower();

            return await _db.Elementos
                .Include(e => e.IdMarcaNavigation)
                .Include(e => e.IdUsuarioNavigation)
                .Include(e => e.RegistrosMovimientos)
                .Where(e =>
                    (e.IdUsuarioNavigation != null && e.IdUsuarioNavigation.Documento != null && e.IdUsuarioNavigation.Documento.ToLower().Contains(q)) ||
                    (e.Serial != null && e.Serial.ToLower().Contains(q)))
                .AsNoTracking()
                .ToListAsync();
        }
         
        public async Task<RegistrosMovimiento> RegistrarEntradaAsync(int idElemento, int idSede, List<int>? detallesPresentes = null)
        {
            // Verificar que no haya un movimiento activo sin salida
            var activo = await _db.RegistrosMovimientos
                .FirstOrDefaultAsync(m => m.IdElemento == idElemento && m.FechaSalida == null);

            if (activo != null)
                throw new InvalidOperationException("Este elemento ya tiene un registro de entrada activo sin salida registrada.");

            var movimiento = new RegistrosMovimiento
            {
                IdElemento = idElemento,
                IdSede = idSede,
                FechaEntrada = DateTime.UtcNow,
                FechaSalida = null
            };

            _db.RegistrosMovimientos.Add(movimiento);
            await _db.SaveChangesAsync();

            // Vincular los accesorios/objetos del elemento a este movimiento
            var detallesElemento = await _db.DetalleElementos
                .Where(d => d.IdElemento == idElemento)
                .ToListAsync();

            if (detallesElemento.Any())
            {
                foreach (var det in detallesElemento)
                {
                    bool isPresente = detallesPresentes == null || detallesPresentes.Contains(det.IdTipoDetalle);
                    _db.MovimientoDetalles.Add(new MovimientoDetalle
                    {
                        IdMovimiento = movimiento.IdMovimiento,
                        IdTipoDetalle = det.IdTipoDetalle,
                        Presente = isPresente
                    });
                }
                await _db.SaveChangesAsync();
            }

            // Notificar al propietario del equipo
            var elemNotif = await _db.Elementos.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdElemento == idElemento);

            if (elemNotif?.IdUsuario != null)
            {
                var sedeNombre = await _db.Sedes.AsNoTracking()
                    .Where(s => s.IdSede == idSede)
                    .Select(s => s.NombreSede)
                    .FirstOrDefaultAsync() ?? "Sede";

                await Controllers.NotificacionesController.CrearAsync(_db,
                    elemNotif.IdUsuario.Value,
                    "entrada",
                    "✅ Equipo ingresado a sede",
                    $"Tu {elemNotif.TipoElemento} fue registrado en {sedeNombre} el {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm}.");
            }

            return movimiento;
        }


        /// <summary>
        /// Registra la salida (Check-Out) en el movimiento activo de un elemento.
        /// </summary>
        public async Task<RegistrosMovimiento> RegistrarSalidaAsync(int idMovimiento)
        {
            var movimiento = await _db.RegistrosMovimientos
                .Include(m => m.IdElementoNavigation)
                .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento)
                ?? throw new KeyNotFoundException("Registro de movimiento no encontrado.");

            if (movimiento.FechaSalida != null)
                throw new InvalidOperationException("Este registro ya tiene una salida registrada.");

            movimiento.FechaSalida = DateTime.UtcNow;

            // Desmarcar el elemento para que no aparezca en control de ingreso hasta que el usuario lo vuelva a seleccionar en Mi Panel
            if (movimiento.IdElementoNavigation != null)
            {
                movimiento.IdElementoNavigation.ActivoParaIngreso = false;
            }
            else
            {
                var elemento = await _db.Elementos.FindAsync(movimiento.IdElemento);
                if (elemento != null)
                {
                    elemento.ActivoParaIngreso = false;
                }
            }

            await _db.SaveChangesAsync();

            // Notificar al propietario
            var idUsuario = movimiento.IdElementoNavigation?.IdUsuario
                ?? await _db.Elementos.AsNoTracking()
                    .Where(e => e.IdElemento == movimiento.IdElemento)
                    .Select(e => e.IdUsuario)
                    .FirstOrDefaultAsync();

            if (idUsuario != null)
            {
                var tipoEquipo = movimiento.IdElementoNavigation?.TipoElemento
                    ?? await _db.Elementos.AsNoTracking()
                        .Where(e => e.IdElemento == movimiento.IdElemento)
                        .Select(e => e.TipoElemento)
                        .FirstOrDefaultAsync() ?? "Equipo";

                await Controllers.NotificacionesController.CrearAsync(_db,
                    idUsuario.Value,
                    "salida",
                    "🚪 Equipo retirado de sede",
                    $"Tu {tipoEquipo} fue retirado el {DateTime.UtcNow.AddHours(-5):dd/MM/yyyy HH:mm}. Ya puedes desmarcarlo en Mi Panel.");
            }

            return movimiento;
        }

        /// <summary>
        /// Obtiene el movimiento activo (sin salida) de un elemento, si existe.
        /// </summary>
        public async Task<RegistrosMovimiento?> GetMovimientoActivoAsync(int idElemento)
        {
            return await _db.RegistrosMovimientos
                .Include(m => m.IdSedeNavigation)
                .OrderByDescending(m => m.FechaEntrada)
                .FirstOrDefaultAsync(m => m.IdElemento == idElemento && m.FechaSalida == null);
        }

        /// <summary>
        /// Obtiene el historial completo de movimientos de un elemento, ordenado por fecha descendente.
        /// </summary>
        public async Task<List<RegistrosMovimiento>> GetHistorialAsync(int idElemento)
        {
            return await _db.RegistrosMovimientos
                .Include(m => m.IdSedeNavigation)
                .Include(m => m.MovimientoDetalles)
                    .ThenInclude(md => md.IdTipoDetalleNavigation)
                .Where(m => m.IdElemento == idElemento)
                .OrderByDescending(m => m.FechaEntrada)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> EliminarMovimientoAsync(int id)
        {
            var detalles = await _db.MovimientoDetalles
                .Where(d => d.IdMovimiento == id)
                .ToListAsync();

            if (detalles.Any())
            {
                _db.MovimientoDetalles.RemoveRange(detalles);
            }

            var movimiento = await _db.RegistrosMovimientos.FindAsync(id);
            if (movimiento == null) return false;

            _db.RegistrosMovimientos.Remove(movimiento);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
