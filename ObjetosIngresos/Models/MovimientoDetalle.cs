using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class MovimientoDetalle
{
    public int IdMovimientoDetalle { get; set; }

    public int IdMovimiento { get; set; }

    public int IdTipoDetalle { get; set; }

    public bool Presente { get; set; }

    public virtual RegistrosMovimiento IdMovimientoNavigation { get; set; } = null!;

    public virtual TiposDetalle IdTipoDetalleNavigation { get; set; } = null!;
}
