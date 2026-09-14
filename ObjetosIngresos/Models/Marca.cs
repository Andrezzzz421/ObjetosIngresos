using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models;

public  class Marca
{
    public int IdMarca { get; set; }

    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$", ErrorMessage = "Las Marcas solo deben contener letras.")]
    public string NombreMarca { get; set; } = null!;

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();
}
