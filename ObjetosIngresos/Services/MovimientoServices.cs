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
         
        public async Task<RegistrosMovimiento> RegistrarEntradaAsync(int idElemento, int idSede)
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
            return movimiento;
        }

        /// <summary>
        /// Registra la salida (Check-Out) en el movimiento activo de un elemento.
        /// </summary>
        public async Task<RegistrosMovimiento> RegistrarSalidaAsync(int idMovimiento)
        {
            var movimiento = await _db.RegistrosMovimientos.FindAsync(idMovimiento)
                ?? throw new KeyNotFoundException("Registro de movimiento no encontrado.");

            if (movimiento.FechaSalida != null)
                throw new InvalidOperationException("Este registro ya tiene una salida registrada.");

            movimiento.FechaSalida = DateTime.UtcNow;
            await _db.SaveChangesAsync();
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
                .Where(m => m.IdElemento == idElemento)
                .OrderByDescending(m => m.FechaEntrada)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
