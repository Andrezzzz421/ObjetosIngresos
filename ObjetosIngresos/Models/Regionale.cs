using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class Regionale
{
    public int IdRegional { get; set; }

    public string NombreRegional { get; set; } = null!;

    public virtual ICollection<CentrosFormacion> CentrosFormacions { get; set; } = new List<CentrosFormacion>();
}
