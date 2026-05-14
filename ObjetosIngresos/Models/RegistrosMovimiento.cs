using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class RegistrosMovimiento
{
    public int IdMovimiento { get; set; }

    public int IdElemento { get; set; }

    public int IdSede { get; set; }

    public DateTime? FechaEntrada { get; set; }

    public DateTime? FechaSalida { get; set; }

    public string? Observaciones { get; set; }

    public virtual Elemento IdElementoNavigation { get; set; } = null!;

    public virtual Sede IdSedeNavigation { get; set; } = null!;

    public virtual ICollection<MovimientoDetalle> MovimientoDetalles { get; set; } = new List<MovimientoDetalle>();
}
