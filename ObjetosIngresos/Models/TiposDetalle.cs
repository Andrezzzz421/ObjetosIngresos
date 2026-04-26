using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public partial class TiposDetalle
{
    public int IdTipoDetalle { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<DetalleElemento> DetalleElementos { get; set; } = new List<DetalleElemento>();

    public virtual ICollection<MovimientoDetalle> MovimientoDetalles { get; set; } = new List<MovimientoDetalle>();
}
