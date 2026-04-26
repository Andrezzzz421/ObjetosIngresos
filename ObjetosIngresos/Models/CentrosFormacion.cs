using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public partial class CentrosFormacion
{
    public int IdCentro { get; set; }

    public int IdRegional { get; set; }

    public string NombreCentro { get; set; } = null!;

    public virtual Regionale IdRegionalNavigation { get; set; } = null!;

    public virtual ICollection<Sede> Sedes { get; set; } = new List<Sede>();
}
