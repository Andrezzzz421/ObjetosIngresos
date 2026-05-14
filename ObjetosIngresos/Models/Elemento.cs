using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class Elemento
{
    public int IdElemento { get; set; }

    public int? IdUsuario { get; set; }

    public string TipoElemento { get; set; } = null!;

    public int? IdMarca { get; set; }

    public string? Serial { get; set; }

    public byte[]? FotoArchivo { get; set; }

    public virtual ICollection<DetalleElemento> DetalleElementos { get; set; } = new List<DetalleElemento>();

    public virtual Marca? IdMarcaNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<RegistrosMovimiento> RegistrosMovimientos { get; set; } = new List<RegistrosMovimiento>();
}
