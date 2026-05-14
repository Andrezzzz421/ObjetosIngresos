using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    public string? Documento { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; }

    [MaxLength(6)]
    public string? codigo_recuperacion { get; set; }

    public DateTime? codigo_expiracion { get; set; }

    public string? Ficha { get; set; }

    public string FirebaseUid { get; set; } = null!;

    public int IdTipoUsuario { get; set; }

    public int? IdSedePrincipal { get; set; }

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();

    public virtual Sede? IdSedePrincipalNavigation { get; set; }

    public virtual TiposUsuario IdTipoUsuarioNavigation { get; set; } = null!;
}
