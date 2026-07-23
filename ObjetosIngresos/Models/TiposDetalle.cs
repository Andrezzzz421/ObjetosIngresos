using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models;

public  class TiposDetalle
{
    public int IdTipoDetalle { get; set; }

    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$", ErrorMessage = "El Nombre solo deben contener letras.")]
    public string Nombre { get; set; } = null!;

    public virtual ICollection<DetalleElemento> DetalleElementos { get; set; } = new List<DetalleElemento>();

    public virtual ICollection<MovimientoDetalle> MovimientoDetalles { get; set; } = new List<MovimientoDetalle>();
}
