using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ObjetosIngresos.Models;

public class Usuario
{
    public int IdUsuario { get; set; }

    [RegularExpression(@"^[0-9]+$", ErrorMessage = "El documento debe contener únicamente números.")]
    [StringLength(20, ErrorMessage = "El documento no puede superar los 20 caracteres.")]
    public string? Documento { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$", ErrorMessage = "Los nombres solo deben contener letras.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombres { get; set; } = null!;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$", ErrorMessage = "Los apellidos solo deben contener letras.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellidos { get; set; } = null!;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
    [StringLength(150)]
    public string Correo { get; set; }

    [MaxLength(6)]
    public string? codigo_recuperacion { get; set; }

    public DateTime? codigo_expiracion { get; set; }

    [RegularExpression(@"^[0-9]+$", ErrorMessage = "La ficha debe contener únicamente números.")]
    [StringLength(20, ErrorMessage = "La ficha no puede superar los 20 caracteres.")]
    public string? Ficha { get; set; }

    public string? FirebaseUid { get; set; } = "";

    [Required(ErrorMessage = "Debe asignar un tipo de usuario.")]
    public int IdTipoUsuario { get; set; }

    public int? IdSedePrincipal { get; set; }

    public virtual ICollection<Elemento> Elementos { get; set; } = new List<Elemento>();

    public virtual Sede? IdSedePrincipalNavigation { get; set; }

    public virtual TiposUsuario? IdTipoUsuarioNavigation { get; set; } = null!;
}
