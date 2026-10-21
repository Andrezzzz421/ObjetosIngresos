using ObjetosIngresos.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ObjetosIngresos.Services
{
    public class HistorialEventoDto
    {
        public string TipoEvento { get; set; } = string.Empty;
        public int? IdMovimiento { get; set; }
        public int? IdPersona { get; set; }
        public int? IdEquipo { get; set; }
        public int? IdUsuarioRegistra { get; set; }
        public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
        public string Descripcion { get; set; } = string.Empty;
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
    }

    public class HistorialFiltroDto
    {
        public string? DocumentoPersona { get; set; }
        public string? SerialEquipo { get; set; }
        public string? TipoEvento { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int Take { get; set; } = 300;
    }

    public class HistorialTimelineItemDto
    {
        public int Id { get; set; }
        public string TipoEvento { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string FechaFormateada { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UsuarioRegistra { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public string ColorClase { get; set; } = string.Empty;
        public string? DatosNuevos { get; set; }
    }

    public class HistorialMovimientoItemDto
    {
        public int IdMovimiento { get; set; }
        public DateTime? FechaEntrada { get; set; }
        public string FechaEntradaTexto { get; set; } = string.Empty;
        public DateTime? FechaSalida { get; set; }
        public string FechaSalidaTexto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public string Persona { get; set; } = string.Empty;
        public string Documento { get; set; } = string.Empty;
        public string? Ficha { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool EsIndependiente { get; set; }
        public string CondicionTexto { get; set; } = string.Empty;

        public string Equipo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;

        public string Sede { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public List<string> Accesorios { get; set; } = new();
        public bool TieneNovedad { get; set; }
    }

    public interface IHistorialService
    {
        /// <summary>
        /// Obtiene el historial consolidado de movimientos (Check-in / Check-out) desde RegistrosMovimiento.
        /// Permite ver cuándo entró y salió el usuario, y si es independiente o no.
        /// </summary>
        Task<List<HistorialMovimientoItemDto>> ObtenerMovimientosHistorialAsync(HistorialFiltroDto? filtro = null);

        /// <summary>
        /// Registra un evento en la bitácora de auditoría (Insert-only).
        /// Si ocurre algún error en el guardado, se loguea la excepción pero NO se interrumpe
        /// ni revoca la transacción del movimiento principal.
        /// </summary>
        Task<bool> RegistrarEventoAsync(HistorialEventoDto dto);

        /// <summary>
        /// Consulta los eventos de auditoría con filtros opcionales.
        /// </summary>
        Task<List<HistorialSistema>> ObtenerHistorialAsync(HistorialFiltroDto? filtro = null);

        /// <summary>
        /// Obtiene la trazabilidad cronológica completa de un IdMovimiento (Entrada -> Novedad/Custodia -> Salida).
        /// </summary>
        Task<List<HistorialTimelineItemDto>> ObtenerTrazabilidadMovimientoAsync(int idMovimiento);

        /// <summary>
        /// Genera el contenido de exportación en formato CSV con codificación UTF-8.
        /// </summary>
        Task<byte[]> ExportarCsvAsync(HistorialFiltroDto? filtro = null);
    }
}
