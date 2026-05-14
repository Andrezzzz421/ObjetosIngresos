using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class Sede
{
    public int IdSede { get; set; }

    public int IdCentro { get; set; }

    public string NombreSede { get; set; } = null!;

    public string? Ciudad { get; set; }

    public virtual CentrosFormacion IdCentroNavigation { get; set; } = null!;

    public virtual ICollection<RegistrosMovimiento> RegistrosMovimientos { get; set; } = new List<RegistrosMovimiento>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
