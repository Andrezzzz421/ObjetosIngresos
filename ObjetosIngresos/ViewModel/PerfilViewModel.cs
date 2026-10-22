using System;
using System.Collections.Generic;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.ViewModel
{
    public class PerfilEquipoItem
    {
        public int IdElemento { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Serial { get; set; } = string.Empty;
        public bool EnSede { get; set; }
        public string IconoTipo { get; set; } = "laptop"; // laptop, mouse, camera, tablet, default
    }

    public class PerfilMovimientoItem
    {
        public int IdMovimiento { get; set; }
        public bool EsIngreso { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Ubicacion { get; set; } = string.Empty;
        public string DescripcionEquipos { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
    }

    public class PerfilViewModel
    {
        public Usuario Usuario { get; set; } = null!;
        public string NombreCompleto { get; set; } = string.Empty;
        public string TipoUsuarioDescripcion { get; set; } = string.Empty;
        public string CodigoUsuario { get; set; } = string.Empty;
        public string? Ficha { get; set; }
        public string SedeNombre { get; set; } = string.Empty;
        public string SedeCiudad { get; set; } = string.Empty;

        // Métricas
        public int TotalEquipos { get; set; }
        public int EquiposEnSede { get; set; }
        public int EquiposFueraDeSede { get; set; }
        public int TotalMovimientos { get; set; }
        public string UltimoIngresoHora { get; set; } = "Sin registros";
        public string UltimoIngresoDetalle { get; set; } = "Sin ingresos recientes";

        // Estado en Sede
        public bool EstaEnSede { get; set; }
        public string EstadoEnSedeTexto { get; set; } = "Fuera de sede";

        // Colecciones (SOLO datos reales)
        public List<PerfilEquipoItem> Equipos { get; set; } = new();
        public List<PerfilMovimientoItem> MovimientosRecientes { get; set; } = new();
    }
}
