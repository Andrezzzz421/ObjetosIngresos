using ObjetosIngresos.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("Movimiento_Novedades")]
public class Movimiento_Novedad
{
    [Key]
    public int id_novedad { get; set; }

    [Required]
    public int id_movimiento { get; set; }

    [Required]
    public int id_usuario_registra { get; set; }

    [Required]
    [MaxLength(100)]
    public string titulo { get; set; } = string.Empty;

    [Required]
    public string descripcion { get; set; } = string.Empty;

    public DateTime fecha_creacion { get; set; } = DateTime.Now;

    // Propiedades de navegación (opcionales para los Joins)
    [ForeignKey("id_movimiento")]
    public virtual RegistrosMovimiento? Movimiento { get; set; }

    [ForeignKey("id_usuario_registra")]
    public virtual Usuario? UsuarioRegistra { get; set; }
}