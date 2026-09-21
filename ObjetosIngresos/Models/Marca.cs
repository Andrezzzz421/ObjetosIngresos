using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class Marca
{
    public int IdMarca { get; set; }

    public string NombreMarca { get; set; } = null!;

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();
}
