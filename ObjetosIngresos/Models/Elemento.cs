using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models;

public  class Elemento
{
    public int IdElemento { get; set; }

    public int? IdUsuario { get; set; }

    [Required(ErrorMessage = "El tipo de elemento es obligatorio.")]
    [StringLength(50, ErrorMessage = "El tipo de elemento no puede superar los 50 caracteres.")]
    public string TipoElemento { get; set; } = null!;

    public int? IdMarca { get; set; }
    [StringLength(50, ErrorMessage = "El número de serial no puede superar los 50 caracteres.")]
    public string? Serial { get; set; }

    public byte[]? FotoArchivo { get; set; }

    public virtual ICollection<DetalleElemento> DetalleElementos { get; set; } = new List<DetalleElemento>();

    public virtual Marca? IdMarcaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<RegistrosMovimiento> RegistrosMovimientos { get; set; } = new List<RegistrosMovimiento>();
}
