using System;
using System.Collections.Generic;

namespace ObjetosIngresos.Models
{
    /// <summary>
    /// Tabla de notificaciones del sistema para los usuarios.
    /// Se genera cuando portería registra entrada/salida de un equipo.
    /// </summary>
    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public int IdUsuario { get; set; }
        /// <summary>Tipo: "entrada", "salida", "sistema"</summary>
        public string Tipo { get; set; } = "sistema";
        public string Titulo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; } = false;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public virtual Usuario? IdUsuarioNavigation { get; set; }
    }
}
