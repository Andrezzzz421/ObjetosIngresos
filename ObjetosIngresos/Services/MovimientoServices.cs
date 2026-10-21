using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class MovimientoServices
    {
        private readonly SistemaIngresoContext _db;
        private readonly IHistorialService? _historialService;

        public MovimientoServices(SistemaIngresoContext db, IHistorialService? historialService = null)
        {
            _db = db;
            _historialService = historialService;
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
         
        public async Task<RegistrosMovimiento> RegistrarEntradaAsync(int idElemento, int idSede, List<int>? detallesPresentes = null, int? idUsuarioRegistra = null)
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
                .Include(d => d.IdTipoDetalleNavigation)
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
        /// Si se indica que el equipo queda en custodia o se reporta alguna novedad/observación,
        /// se crea automáticamente un registro en la tabla Movimiento_Novedades vinculado al id_movimiento.
        /// </summary>
        public async Task<RegistrosMovimiento> RegistrarSalidaAsync(
            int idMovimiento,
            int? idUsuarioRegistra = null,
            bool dejaEnCustodia = false,
            string? observaciones = null,
            string? tituloNovedad = null)
        {
            var movimiento = await _db.RegistrosMovimientos
                .Include(m => m.IdElementoNavigation)
                .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento)
                ?? throw new KeyNotFoundException("Registro de movimiento no encontrado.");

            if (movimiento.FechaSalida != null)
                throw new InvalidOperationException("Este registro ya tiene una salida registrada.");

            movimiento.FechaSalida = DateTime.UtcNow;

            // Actualizar observaciones en el movimiento si se suministraron
            if (!string.IsNullOrWhiteSpace(observaciones))
            {
                movimiento.Observaciones = string.IsNullOrWhiteSpace(movimiento.Observaciones)
                    ? observaciones.Trim()
                    : $"{movimiento.Observaciones} | {observaciones.Trim()}";
            }
            else if (dejaEnCustodia && string.IsNullOrWhiteSpace(movimiento.Observaciones))
            {
                movimiento.Observaciones = "Salida con equipo dejado en custodia temporal.";
            }

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

            // Determinar el id_usuario_registra asegurando integridad con PostgreSQL
            int resolvedUserId = idUsuarioRegistra ?? 0;
            if (resolvedUserId <= 0)
            {
                resolvedUserId = movimiento.IdElementoNavigation?.IdUsuario
                    ?? await _db.Elementos.Where(e => e.IdElemento == movimiento.IdElemento).Select(e => e.IdUsuario).FirstOrDefaultAsync()
                    ?? await _db.Usuarios.Select(u => u.IdUsuario).FirstOrDefaultAsync();
            }

            // Integración de Salidas Parciales / Custodia y Novedades en Movimiento_Novedades
            Movimiento_Novedad? novedadCreada = null;
            if (dejaEnCustodia || !string.IsNullOrWhiteSpace(observaciones) || !string.IsNullOrWhiteSpace(tituloNovedad))
            {
                string tituloFinal;
                if (!string.IsNullOrWhiteSpace(tituloNovedad))
                {
                    tituloFinal = tituloNovedad.Trim();
                }
                else if (dejaEnCustodia)
                {
                    tituloFinal = "Salida sin equipo (Custodia)";
                }
                else
                {
                    tituloFinal = "Novedad en Salida";
                }

                if (tituloFinal.Length > 100)
                {
                    tituloFinal = tituloFinal.Substring(0, 100);
                }

                string descripcionFinal;
                if (dejaEnCustodia)
                {
                    if (!string.IsNullOrWhiteSpace(observaciones))
                    {
                        descripcionFinal = $"Salida de usuario sin equipo - dejado en custodia. Detalle: {observaciones.Trim()}";
                    }
                    else
                    {
                        descripcionFinal = "Salida de usuario sin equipo - dejado en custodia";
                    }
                }
                else
                {
                    descripcionFinal = observaciones?.Trim() ?? "Salida registrada con novedades u observaciones.";
                }

                novedadCreada = new Movimiento_Novedad
                {
                    id_movimiento = movimiento.IdMovimiento,
                    id_usuario_registra = resolvedUserId,
                    titulo = tituloFinal,
                    descripcion = descripcionFinal,
                    fecha_creacion = DateTime.UtcNow
                };

                _db.Movimiento_Novedades.Add(novedadCreada);
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
        /// Obtiene las novedades registradas para una lista de movimientos.
        /// </summary>
        public async Task<List<Movimiento_Novedad>> GetNovedadesPorMovimientosAsync(List<int> idsMovimientos)
        {
            if (idsMovimientos == null || !idsMovimientos.Any()) return new List<Movimiento_Novedad>();

            return await _db.Movimiento_Novedades
                .Include(n => n.UsuarioRegistra)
                .Where(n => idsMovimientos.Contains(n.id_movimiento))
                .OrderByDescending(n => n.fecha_creacion)
                .AsNoTracking()
                .ToListAsync();
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

            // Registro automático de auditoría: Anulación
            if (_historialService != null)
            {
                try
                {
                    await _historialService.RegistrarEventoAsync(new HistorialEventoDto
                    {
                        TipoEvento = "Anulacion",
                        IdMovimiento = id,
                        IdEquipo = movimiento.IdElemento,
                        FechaEvento = DateTime.UtcNow,
                        Descripcion = $"Anulación de registro de movimiento #{id}."
                    });
                }
                catch
                {
                }
            }

            return true;
        }
    }
}
