using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public partial class Marca
{
    public int IdMarca { get; set; }

    public string NombreMarca { get; set; } = null!;

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();
}
