using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models
{
    public class PreRegistroViewModel
    {
        [Required(ErrorMessage = "El documento es obligatorio.")]
        public string Documento { get; set; } = null!;

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        public string Nombres { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo válido.")]
        public string Correo { get; set; } = null!;

        [Required(ErrorMessage = "El tipo de elemento es obligatorio (ej. Laptop).")]
        public string TipoElemento { get; set; } = null!;

        public int? IdMarca { get; set; }

        public string? Serial { get; set; }
    }
}
