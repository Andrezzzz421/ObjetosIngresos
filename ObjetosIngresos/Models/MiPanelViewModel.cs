using System.Collections.Generic;

namespace ObjetosIngresos.Models
{
    public class MiPanelEquipoViewModel
    {
        public int IdElemento { get; set; }
        public string TipoElemento { get; set; } = string.Empty;
        public string? Serial { get; set; }
        public string NombreMarca { get; set; } = "Sin marca";
        public bool TieneFoto { get; set; }
        public bool TieneIngresoActivo { get; set; }
    }

    public class MiPanelViewModel
    {
        public string Nombres { get; set; } = string.Empty;
        public List<MiPanelEquipoViewModel> Elementos { get; set; } = new();
    }
}
