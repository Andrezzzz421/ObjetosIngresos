using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models;

public  class TiposUsuario
{
    public int IdTipoUsuario { get; set; }

    public string Descripcion { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
