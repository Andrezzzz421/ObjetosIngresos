using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.ViewModel
{
    public class SalidaViewModel
    {
        [Required]
        public int IdMovimiento { get; set; }

        /// <summary>
        /// Indica si el usuario sale sin el equipo y este queda en custodia temporal en la sede.
        /// </summary>
        public bool DejaEnCustodia { get; set; } = false;

        /// <summary>
        /// Indica si se desea registrar una novedad o daño durante la salida normal.
        /// </summary>
        public bool RegistrarNovedad { get; set; } = false;

        [MaxLength(100)]
        public string? TituloNovedad { get; set; }

        public string? Observaciones { get; set; }
    }
}
