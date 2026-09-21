using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class DetalleElemento
{
    public int IdDetalle { get; set; }

    public int IdElemento { get; set; }

    public int IdTipoDetalle { get; set; }

    public virtual Elemento IdElementoNavigation { get; set; } = null!;

    public virtual TiposDetalle IdTipoDetalleNavigation { get; set; } = null!;
}
