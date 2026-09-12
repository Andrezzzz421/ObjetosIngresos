using System.Collections.Generic;

namespace ObjetosIngresos.Models
{
    public class ElementoIndexViewModel
    {
        public int IdElemento { get; set; }
        public string TipoElemento { get; set; } = string.Empty;
        public string? Serial { get; set; }
        public bool TieneFoto { get; set; }

        public string PropietarioNombres { get; set; } = string.Empty;
        public string PropietarioApellidos { get; set; } = string.Empty;
        public string PropietarioDocumento { get; set; } = "Sin documento";

        public string NombreMarca { get; set; } = "Sin Marca";

        public List<string> Accesorios { get; set; } = new();
    }
}
