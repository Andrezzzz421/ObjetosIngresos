using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ObjetosIngresos.Models
{
    [Table("HistorialSistema")]
    public class HistorialSistema
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoEvento { get; set; } = string.Empty;

        public int? IdMovimiento { get; set; }

        public int? IdPersona { get; set; }

        public int? IdEquipo { get; set; }

        public int? IdUsuarioRegistra { get; set; }

        public DateTime FechaEvento { get; set; } = DateTime.UtcNow;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        public string? DatosAnteriores { get; set; }

        public string? DatosNuevos { get; set; }

        // Propiedades de navegación (opcionales)
        [ForeignKey("IdMovimiento")]
        public virtual RegistrosMovimiento? Movimiento { get; set; }

        [ForeignKey("IdPersona")]
        public virtual Usuario? Persona { get; set; }

        [ForeignKey("IdEquipo")]
        public virtual Elemento? Equipo { get; set; }

        [ForeignKey("IdUsuarioRegistra")]
        public virtual Usuario? UsuarioRegistra { get; set; }
    }
}
