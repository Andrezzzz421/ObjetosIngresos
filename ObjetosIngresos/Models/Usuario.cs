using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace ObjetosIngresos.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    [RegularExpression(@"^[0-9]+$", ErrorMessage = "El documento debe contener únicamente números.")]
    [StringLength(20, ErrorMessage = "El documento no puede superar los 20 caracteres.")]
    [Remote(action: "ValidarDocumento", controller: "Usuarios", AdditionalFields = nameof(IdUsuario))]
    public string? Documento { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(150)]
    [Remote(action: "ValidarCorreo", controller: "Usuarios", AdditionalFields = nameof(IdUsuario))]
    public string Correo { get; set; }

    public string? CodigoRecuperacion { get; set; }
    public string? codigo_recuperacion { get => CodigoRecuperacion; set => CodigoRecuperacion = value; }

    public DateTime? CodigoExpiracion { get; set; }
    public DateTime? codigo_expiracion { get => CodigoExpiracion; set => CodigoExpiracion = value; }

    public string? Ficha { get; set; }

    public string? FirebaseUid { get; set; }

    public int IdTipoUsuario { get; set; }

    public int? IdSedePrincipal { get; set; }

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();

    public virtual Sede? IdSedePrincipalNavigation { get; set; }

    public virtual TiposUsuario IdTipoUsuarioNavigation { get; set; } = null!;
}
