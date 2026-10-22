using System;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.ViewModel
{
    public class PreRegistroViewModel
    {
        [Required(ErrorMessage = "El documento es obligatorio.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El documento debe contener únicamente números.")]
        [StringLength(20, ErrorMessage = "El documento no puede superar los 20 dígitos.")]
        [Display(Name = "Número de Documento")]
        public string Documento { get; set; } = null!;

        [Required(ErrorMessage = "Los nombres son obligatorios.")]
        [StringLength(100, ErrorMessage = "Los nombres no pueden superar los 100 caracteres.")]
        [Display(Name = "Nombres")]
        public string Nombres { get; set; } = null!;

        [Required(ErrorMessage = "Los apellidos son obligatorios.")]
        [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
        [Display(Name = "Apellidos")]
        public string Apellidos { get; set; } = null!;

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
        [StringLength(150, ErrorMessage = "El correo no puede superar los 150 caracteres.")]
        [Display(Name = "Correo Electrónico")]
        public string Correo { get; set; } = null!;
    }

    public class PreRegistroCodigoViewModel
    {
        [Required]
        public string Correo { get; set; } = null!;

        [Required(ErrorMessage = "El código es obligatorio.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "El código debe contener exactamente 6 dígitos numéricos.")]
        public string Codigo { get; set; } = null!;
    }

    public class PreRegistroDatosTemporales
    {
        public string Documento { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string Correo { get; set; } = null!;
        public string Codigo { get; set; } = null!;
        public DateTime ExpiracionUtc { get; set; }
    }
}

