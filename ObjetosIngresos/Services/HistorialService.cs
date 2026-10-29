using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ObjetosIngresos.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjetosIngresos.Services
{
    public class HistorialService : IHistorialService
    {
        private readonly SistemaIngresoContext _db;
        private readonly ILogger<HistorialService> _logger;

        public HistorialService(SistemaIngresoContext db, ILogger<HistorialService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Registra un evento en la bitácora de auditoría (Insert-only).
        /// Si ocurre algún error en el guardado, se loguea la excepción pero NO se interrumpe
        /// ni revoca la transacción del movimiento principal.
        /// </summary>
        public async Task<bool> RegistrarEventoAsync(HistorialEventoDto dto)
        {
            if (dto == null) return false;

            try
            {
                var fechaEvUtc = dto.FechaEvento.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dto.FechaEvento, DateTimeKind.Utc)
                    : dto.FechaEvento.ToUniversalTime();

                var evento = new HistorialSistema
                {
                    TipoEvento = string.IsNullOrWhiteSpace(dto.TipoEvento) ? "Evento" : dto.TipoEvento.Trim(),
                    IdMovimiento = dto.IdMovimiento,
                    IdPersona = dto.IdPersona,
                    IdEquipo = dto.IdEquipo,
                    IdUsuarioRegistra = dto.IdUsuarioRegistra,
                    FechaEvento = fechaEvUtc,
                    FechaRegistro = DateTime.UtcNow,
                    Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? "Sin descripción" : dto.Descripcion.Trim(),
                    DatosAnteriores = dto.DatosAnteriores,
                    DatosNuevos = dto.DatosNuevos
                };

                _db.HistorialSistema.Add(evento);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // REGLA ESTRICTA: Loguear excepción sin interrumpir el flujo del movimiento principal
                _logger.LogError(ex, "Error al registrar evento de auditoría en HistorialSistema: {Mensaje}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Obtiene el historial consolidado de movimientos (Check-in / Check-out) desde RegistrosMovimiento.
        /// Muestra el registro completo de entrada y salida, con identificación de aprendiz / independiente.
        /// </summary>
        public async Task<List<HistorialMovimientoItemDto>> ObtenerMovimientosHistorialAsync(HistorialFiltroDto? filtro = null)
        {
            try
            {
                var query = _db.RegistrosMovimientos
                    .Include(m => m.IdElementoNavigation)
                        .ThenInclude(e => e.IdMarcaNavigation)
                    .Include(m => m.IdElementoNavigation)
                        .ThenInclude(e => e.IdUsuarioNavigation)
                            .ThenInclude(u => u!.IdTipoUsuarioNavigation)
                    .Include(m => m.IdSedeNavigation)
                    .Include(m => m.MovimientoDetalles)
                        .ThenInclude(d => d.IdTipoDetalleNavigation)
                    .AsNoTracking()
                    .AsQueryable();

                if (filtro != null)
                {
                    if (!string.IsNullOrWhiteSpace(filtro.DocumentoPersona))
                    {
                        var doc = filtro.DocumentoPersona.Trim().ToLower();
                        query = query.Where(m => m.IdElementoNavigation != null &&
                                                 m.IdElementoNavigation.IdUsuarioNavigation != null &&
                                                 m.IdElementoNavigation.IdUsuarioNavigation.Documento != null &&
                                                 m.IdElementoNavigation.IdUsuarioNavigation.Documento.ToLower().Contains(doc));
                    }

                    if (!string.IsNullOrWhiteSpace(filtro.SerialEquipo))
                    {
                        var s = filtro.SerialEquipo.Trim().ToLower();
                        query = query.Where(m => m.IdElementoNavigation != null &&
                                                 m.IdElementoNavigation.Serial != null &&
                                                 m.IdElementoNavigation.Serial.ToLower().Contains(s));
                    }

                    if (!string.IsNullOrWhiteSpace(filtro.TipoEvento) && filtro.TipoEvento != "Todos")
                    {
                        var tipo = filtro.TipoEvento.Trim().ToLower();
                        if (tipo == "entrada" || tipo == "ensede" || tipo == "pendiente")
                        {
                            query = query.Where(m => m.FechaSalida == null);
                        }
                        else if (tipo == "salida" || tipo == "salidaequipo")
                        {
                            query = query.Where(m => m.FechaSalida != null);
                        }
                        else if (tipo == "custodia" || tipo == "salidapersona")
                        {
                            query = query.Where(m => m.Observaciones != null && m.Observaciones.ToLower().Contains("custodia"));
                        }
                        else if (tipo == "novedad")
                        {
                            query = query.Where(m => m.Observaciones != null && m.Observaciones.Trim() != "");
                        }
                    }

                    if (filtro.FechaDesde.HasValue)
                    {
                        var desdeUtc = DateTime.SpecifyKind(filtro.FechaDesde.Value.Date.AddHours(5), DateTimeKind.Utc);
                        query = query.Where(m => m.FechaEntrada >= desdeUtc);
                    }

                    if (filtro.FechaHasta.HasValue)
                    {
                        var hastaUtc = DateTime.SpecifyKind(filtro.FechaHasta.Value.Date.AddHours(29), DateTimeKind.Utc); // Fin del día UTC-5
                        query = query.Where(m => m.FechaEntrada <= hastaUtc);
                    }
                }

                var limite = filtro?.Take > 0 ? filtro.Take : 300;
                var listaDb = await query.OrderByDescending(m => m.IdMovimiento)
                    .Take(limite)
                    .ToListAsync();

                var resultado = new List<HistorialMovimientoItemDto>();

                foreach (var mov in listaDb)
                {
                    var elem = mov.IdElementoNavigation;
                    var user = elem?.IdUsuarioNavigation;
                    var rolDesc = user?.IdTipoUsuarioNavigation?.Descripcion ?? "";
                    var rolDescLower = rolDesc.ToLower();

                    bool esIndependiente = false;
                    string condicion = "Aprendiz SENA";

                    if (user == null)
                    {
                        esIndependiente = true;
                        condicion = "Independiente";
                    }
                    else if (rolDescLower.Contains("aprendiz"))
                    {
                        esIndependiente = false;
                        condicion = !string.IsNullOrWhiteSpace(user.Ficha) ? $"Aprendiz (Ficha: {user.Ficha})" : "Aprendiz SENA";
                    }
                    else if (rolDescLower.Contains("independiente") || rolDescLower.Contains("visitante") || rolDescLower.Contains("guest") || string.IsNullOrWhiteSpace(user.Ficha))
                    {
                        esIndependiente = true;
                        condicion = string.IsNullOrWhiteSpace(rolDesc) ? "Independiente" : rolDesc;
                    }
                    else
                    {
                        esIndependiente = false;
                        condicion = rolDesc;
                    }

                    string estado = "Salida realizada";
                    bool esCustodia = mov.Observaciones != null && mov.Observaciones.ToLower().Contains("custodia");

                    if (mov.FechaSalida.HasValue)
                    {
                        estado = esCustodia ? "Salida (Custodia)" : "Salida realizada";
                    }
                    else
                    {
                        estado = esCustodia ? "En custodia" : "En sede (Activo)";
                    }

                    var detallesAccesorios = mov.MovimientoDetalles
                        .Where(d => d.Presente)
                        .Select(d => d.IdTipoDetalleNavigation?.Nombre ?? "Accesorio")
                        .ToList();

                    resultado.Add(new HistorialMovimientoItemDto
                    {
                        IdMovimiento = mov.IdMovimiento,
                        FechaEntrada = mov.FechaEntrada,
                        FechaEntradaTexto = mov.FechaEntrada.HasValue
                            ? mov.FechaEntrada.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt")
                            : "—",
                        FechaSalida = mov.FechaSalida,
                        FechaSalidaTexto = mov.FechaSalida.HasValue
                            ? mov.FechaSalida.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm tt")
                            : "En sede (Dentro)",
                        Estado = estado,
                        Persona = user != null ? $"{user.Nombres} {user.Apellidos}".Trim() : "Independiente / Sin asignar",
                        Documento = user?.Documento ?? "N/A",
                        Ficha = user?.Ficha,
                        Rol = string.IsNullOrWhiteSpace(rolDesc) ? (esIndependiente ? "Independiente" : "Aprendiz") : rolDesc,
                        EsIndependiente = esIndependiente,
                        CondicionTexto = condicion,
                        Equipo = elem?.TipoElemento ?? "Equipo",
                        Marca = elem?.IdMarcaNavigation?.NombreMarca ?? "Sin marca",
                        Serial = elem?.Serial ?? "N/A",
                        Sede = mov.IdSedeNavigation?.NombreSede ?? "Sede SENA",
                        Observaciones = mov.Observaciones,
                        Accesorios = detallesAccesorios,
                        TieneNovedad = !string.IsNullOrWhiteSpace(mov.Observaciones)
                    });
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar movimientos en RegistrosMovimiento: {Mensaje}", ex.Message);
                return new List<HistorialMovimientoItemDto>();
            }
        }

        /// <summary>
        /// Consulta los eventos de auditoría con filtros opcionales.
        /// </summary>
        public async Task<List<HistorialSistema>> ObtenerHistorialAsync(HistorialFiltroDto? filtro = null)
        {
            try
            {
                var query = _db.HistorialSistema
                    .Include(h => h.Persona)
                    .Include(h => h.Equipo)
                        .ThenInclude(e => e!.IdMarcaNavigation)
                    .Include(h => h.UsuarioRegistra)
                    .Include(h => h.Movimiento)
                        .ThenInclude(m => m!.IdSedeNavigation)
                    .AsNoTracking()
                    .AsQueryable();

                if (filtro != null)
                {
                    if (!string.IsNullOrWhiteSpace(filtro.DocumentoPersona))
                    {
                        var doc = filtro.DocumentoPersona.Trim().ToLower();
                        query = query.Where(h => h.Persona != null && h.Persona.Documento != null && h.Persona.Documento.ToLower().Contains(doc));
                    }

                    if (!string.IsNullOrWhiteSpace(filtro.SerialEquipo))
                    {
                        var s = filtro.SerialEquipo.Trim().ToLower();
                        query = query.Where(h => h.Equipo != null && h.Equipo.Serial != null && h.Equipo.Serial.ToLower().Contains(s));
                    }

                    if (!string.IsNullOrWhiteSpace(filtro.TipoEvento) && filtro.TipoEvento != "Todos")
                    {
                        var tipo = filtro.TipoEvento.Trim().ToLower();
                        query = query.Where(h => h.TipoEvento.ToLower() == tipo);
                    }

                    if (filtro.FechaDesde.HasValue)
                    {
                        var desdeUtc = DateTime.SpecifyKind(filtro.FechaDesde.Value.Date.AddHours(5), DateTimeKind.Utc);
                        query = query.Where(h => h.FechaEvento >= desdeUtc);
                    }

                    if (filtro.FechaHasta.HasValue)
                    {
                        var hastaUtc = DateTime.SpecifyKind(filtro.FechaHasta.Value.Date.AddHours(29), DateTimeKind.Utc); // Fin del día en UTC-5
                        query = query.Where(h => h.FechaEvento <= hastaUtc);
                    }
                }

                var limite = filtro?.Take > 0 ? filtro.Take : 300;
                return await query.OrderByDescending(h => h.FechaEvento)
                    .Take(limite)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar HistorialSistema: {Mensaje}", ex.Message);
                return new List<HistorialSistema>();
            }
        }

        /// <summary>
        /// Obtiene la trazabilidad cronológica completa de un IdMovimiento (Entrada -> Novedad/Custodia -> Salida).
        /// Combina eventos registrados en la bitácora con los datos reales de RegistrosMovimiento y Novedades.
        /// </summary>
        public async Task<List<HistorialTimelineItemDto>> ObtenerTrazabilidadMovimientoAsync(int idMovimiento)
        {
            var timeline = new List<HistorialTimelineItemDto>();

            try
            {
                // 1. Obtener eventos registrados en HistorialSistema
                var eventos = await _db.HistorialSistema
                    .Include(h => h.UsuarioRegistra)
                    .Where(h => h.IdMovimiento == idMovimiento)
                    .OrderBy(h => h.FechaEvento)
                    .AsNoTracking()
                    .ToListAsync();

                if (eventos.Any())
                {
                    foreach (var ev in eventos)
                    {
                        var infoEstilo = ObtenerEstiloEvento(ev.TipoEvento);
                        timeline.Add(new HistorialTimelineItemDto
                        {
                            Id = ev.Id,
                            TipoEvento = ev.TipoEvento,
                            Titulo = infoEstilo.Titulo,
                            Fecha = ev.FechaEvento,
                            FechaFormateada = ev.FechaEvento.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt"),
                            Descripcion = ev.Descripcion,
                            UsuarioRegistra = ev.UsuarioRegistra != null ? $"{ev.UsuarioRegistra.Nombres} {ev.UsuarioRegistra.Apellidos}".Trim() : "Personal SENA",
                            Icono = infoEstilo.Icono,
                            ColorClase = infoEstilo.ColorClase,
                            DatosNuevos = ev.DatosNuevos
                        });
                    }
                }

                // 2. Si no hay eventos en la bitácora o para complementar movimientos anteriores a la bitácora:
                if (!timeline.Any())
                {
                    var mov = await _db.RegistrosMovimientos
                        .Include(m => m.IdElementoNavigation)
                            .ThenInclude(e => e.IdUsuarioNavigation)
                        .Include(m => m.IdSedeNavigation)
                        .Include(m => m.MovimientoDetalles)
                            .ThenInclude(d => d.IdTipoDetalleNavigation)
                        .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento);

                    if (mov != null)
                    {
                        // Entrada
                        if (mov.FechaEntrada.HasValue)
                        {
                            var detalles = mov.MovimientoDetalles.Where(d => d.Presente).Select(d => d.IdTipoDetalleNavigation?.Nombre ?? "Accesorio").ToList();
                            var textoAccesorios = detalles.Any() ? $" con accesorios: {string.Join(", ", detalles)}" : " sin accesorios reportados";

                            timeline.Add(new HistorialTimelineItemDto
                            {
                                Id = mov.IdMovimiento,
                                TipoEvento = "Entrada",
                                Titulo = "Ingreso registrado (Check-In)",
                                Fecha = mov.FechaEntrada.Value,
                                FechaFormateada = mov.FechaEntrada.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt"),
                                Descripcion = $"Ingreso a sede {mov.IdSedeNavigation?.NombreSede ?? "SENA"}{textoAccesorios}.",
                                UsuarioRegistra = "Recepción",
                                Icono = "log-in",
                                ColorClase = "bg-indigo-50 border-indigo-200 text-indigo-700"
                            });
                        }

                        // Novedades vinculadas
                        var novedades = await _db.Movimiento_Novedades
                            .Include(n => n.UsuarioRegistra)
                            .Where(n => n.id_movimiento == idMovimiento)
                            .OrderBy(n => n.fecha_creacion)
                            .ToListAsync();

                        foreach (var nov in novedades)
                        {
                            var esCustodia = nov.titulo.Contains("Custodia", StringComparison.OrdinalIgnoreCase);
                            timeline.Add(new HistorialTimelineItemDto
                            {
                                Id = nov.id_novedad,
                                TipoEvento = esCustodia ? "SalidaPersona" : "Novedad",
                                Titulo = nov.titulo,
                                Fecha = nov.fecha_creacion,
                                FechaFormateada = nov.fecha_creacion.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt"),
                                Descripcion = nov.descripcion,
                                UsuarioRegistra = nov.UsuarioRegistra != null ? $"{nov.UsuarioRegistra.Nombres} {nov.UsuarioRegistra.Apellidos}".Trim() : "Personal SENA",
                                Icono = esCustodia ? "package" : "alert-triangle",
                                ColorClase = esCustodia ? "bg-amber-50 border-amber-200 text-amber-700" : "bg-rose-50 border-rose-200 text-rose-700"
                            });
                        }

                        // Salida
                        if (mov.FechaSalida.HasValue)
                        {
                            var tieneCustodia = novedades.Any(n => n.titulo.Contains("Custodia", StringComparison.OrdinalIgnoreCase));
                            timeline.Add(new HistorialTimelineItemDto
                            {
                                Id = mov.IdMovimiento,
                                TipoEvento = tieneCustodia ? "SalidaPersona" : "SalidaEquipo",
                                Titulo = tieneCustodia ? "Salida sin equipo (Custodia)" : "Salida con equipo (Check-Out)",
                                Fecha = mov.FechaSalida.Value,
                                FechaFormateada = mov.FechaSalida.Value.AddHours(-5).ToString("dd/MM/yyyy hh:mm:ss tt"),
                                Descripcion = string.IsNullOrWhiteSpace(mov.Observaciones) ? "Salida completada correctamente." : mov.Observaciones,
                                UsuarioRegistra = "Recepción",
                                Icono = tieneCustodia ? "package" : "log-out",
                                ColorClase = tieneCustodia ? "bg-amber-50 border-amber-200 text-amber-700" : "bg-emerald-50 border-emerald-200 text-emerald-700"
                            });
                        }
                    }
                }

                return timeline.OrderBy(t => t.Fecha).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener trazabilidad del movimiento {IdMovimiento}: {Mensaje}", idMovimiento, ex.Message);
                return timeline;
            }
        }

        /// <summary>
        /// Genera el contenido de exportación en formato CSV con codificación UTF-8 BOM.
        /// </summary>
        public async Task<byte[]> ExportarCsvAsync(HistorialFiltroDto? filtro = null)
        {
            var movimientos = await ObtenerMovimientosHistorialAsync(filtro);
            var sb = new StringBuilder();

            // Encabezados CSV con formato estándar Excel (punto y coma como delimitador habitual en español)
            sb.AppendLine("ID Movimiento;Fecha Entrada;Fecha Salida;Estado;Persona / Propietario;Documento;Ficha;¿Es Independiente?;Condición / Rol;Equipo;Marca;Serial;Sede;Accesorios;Observaciones");

            foreach (var m in movimientos)
            {
                var id = m.IdMovimiento.ToString();
                var entrada = EscapeCsv(m.FechaEntradaTexto);
                var salida = EscapeCsv(m.FechaSalidaTexto);
                var estado = EscapeCsv(m.Estado);
                var persona = EscapeCsv(m.Persona);
                var doc = EscapeCsv(m.Documento);
                var ficha = EscapeCsv(m.Ficha ?? "N/A");
                var esIndep = EscapeCsv(m.EsIndependiente ? "SÍ" : "NO");
                var condicion = EscapeCsv(m.CondicionTexto);
                var equipo = EscapeCsv(m.Equipo);
                var marca = EscapeCsv(m.Marca);
                var serial = EscapeCsv(m.Serial);
                var sede = EscapeCsv(m.Sede);
                var accesorios = EscapeCsv(string.Join(", ", m.Accesorios));
                var obs = EscapeCsv(m.Observaciones ?? "Ninguna");

                sb.AppendLine($"{id};{entrada};{salida};{estado};{persona};{doc};{ficha};{esIndep};{condicion};{equipo};{marca};{serial};{sede};{accesorios};{obs}");
            }

            // Prefijo BOM (Byte Order Mark) UTF-8 para garantizar que Excel abra tildes y caracteres especiales correctamente
            var bom = Encoding.UTF8.GetPreamble();
            var contenidoBytes = Encoding.UTF8.GetBytes(sb.ToString());
            var resultadoFinal = new byte[bom.Length + contenidoBytes.Length];

            Buffer.BlockCopy(bom, 0, resultadoFinal, 0, bom.Length);
            Buffer.BlockCopy(contenidoBytes, 0, resultadoFinal, bom.Length, contenidoBytes.Length);

            return resultadoFinal;
        }

        private static string EscapeCsv(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return "\"\"";
            var limpio = texto.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
            return $"\"{limpio}\"";
        }

        private static (string Titulo, string Icono, string ColorClase) ObtenerEstiloEvento(string tipoEvento)
        {
            return tipoEvento?.Trim().ToLower() switch
            {
                "entrada" => ("Ingreso registrado (Check-In)", "log-in", "bg-indigo-50 border-indigo-200 text-indigo-700"),
                "salidaequipo" => ("Salida con equipo (Check-Out)", "log-out", "bg-emerald-50 border-emerald-200 text-emerald-700"),
                "salidapersona" => ("Salida sin equipo (Custodia)", "package", "bg-amber-50 border-amber-200 text-amber-700"),
                "novedad" => ("Novedad reportada", "alert-triangle", "bg-rose-50 border-rose-200 text-rose-700"),
                "modificacion" => ("Modificación de registro", "edit", "bg-purple-50 border-purple-200 text-purple-700"),
                "anulacion" => ("Anulación de movimiento", "x-circle", "bg-slate-100 border-slate-300 text-slate-700"),
                _ => (tipoEvento ?? "Evento del sistema", "activity", "bg-slate-50 border-slate-200 text-slate-600")
            };
        }
    }
}
