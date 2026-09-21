using System.Collections.Generic;

namespace ObjetosIngresos.ViewModel
{
    public class AdminDashboardViewModel
    {
        public string NombreAdmin { get; set; } = "Administrador";
        public string FechaHoy { get; set; } = string.Empty;

        public AdminKpiDto Kpis { get; set; } = new();
        public List<AdminMovimientoDto> UltimosMovimientos { get; set; } = new();
        public List<AdminDistribucionEquipoDto> DistribucionEquipos { get; set; } = new();
        public List<AdminRolDto> RolesUsuarios { get; set; } = new();
        public List<AdminSedeDto> SedesActivas { get; set; } = new();
        public List<AdminUsuarioDto> UltimosUsuarios { get; set; } = new();
    }

    public class AdminKpiDto
    {
        public int TotalMovimientos { get; set; }
        public int EquiposDentro { get; set; }
        public int IngresoHoy { get; set; }
        public int SalidaHoy { get; set; }
        public int RetornoHoyPct { get; set; }
        public int TotalElementos { get; set; }
        public int TotalAccesorios { get; set; }
        public int ElementosSinMovimiento { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalSedes { get; set; }
        public int TotalCentros { get; set; }
        public int TotalRegionales { get; set; }
        public int TotalMarcas { get; set; }
    }

    public class AdminMovimientoDto
    {
        public int Id { get; set; }
        public string Propietario { get; set; } = "Desconocido";
        public string Documento { get; set; } = "—";
        public string Equipo { get; set; } = "—";
        public string Serial { get; set; } = "S/N";
        public string Sede { get; set; } = "—";
        public string FechaEntrada { get; set; } = "—";
        public string FechaSalida { get; set; } = "—";
        public bool Activo { get; set; }
    }

    public class AdminDistribucionEquipoDto
    {
        public string Tipo { get; set; } = "Sin tipo";
        public int Cantidad { get; set; }
        public int Porcentaje { get; set; }
    }

    public class AdminRolDto
    {
        public string Rol { get; set; } = "Sin rol";
        public int Cantidad { get; set; }
    }

    public class AdminSedeDto
    {
        public string Sede { get; set; } = "—";
        public int Total { get; set; }
        public int Porcentaje { get; set; }
    }

    public class AdminUsuarioDto
    {
        public string Nombre { get; set; } = "—";
        public string Inicial { get; set; } = "?";
        public string Documento { get; set; } = "—";
        public string Correo { get; set; } = string.Empty;
        public string Rol { get; set; } = "—";
        public string Sede { get; set; } = "—";
    }
}
