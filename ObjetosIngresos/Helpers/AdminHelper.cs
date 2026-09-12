using System;

namespace ObjetosIngresos.Helpers
{
    public static class AdminHelper
    {
        /// <summary>
        /// Obtiene la inicial mayúscula de un nombre para avatares de usuario.
        /// </summary>
        public static string GetInitial(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "?";

            return name.Trim().Substring(0, 1).ToUpper();
        }

        /// <summary>
        /// Retorna clases de Tailwind CSS para colorear los badges de roles.
        /// </summary>
        public static string GetRolBadgeClass(string? rol)
        {
            if (string.IsNullOrWhiteSpace(rol))
                return "bg-slate-100 text-slate-700";

            return rol.ToLower() switch
            {
                var r when r.Contains("admin") => "bg-indigo-50 text-indigo-700 border border-indigo-200/50",
                var r when r.Contains("instructor") => "bg-emerald-50 text-emerald-700 border border-emerald-200/50",
                var r when r.Contains("aprendiz") => "bg-amber-50 text-amber-700 border border-amber-200/50",
                var r when r.Contains("funcionario") => "bg-sky-50 text-sky-700 border border-sky-200/50",
                _ => "bg-purple-50 text-purple-700 border border-purple-200/50"
            };
        }

        /// <summary>
        /// Retorna un gradiente de fondo para avatares según el primer carácter del nombre.
        /// </summary>
        public static string GetAvatarGradient(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "from-slate-500 to-slate-700";

            char first = char.ToUpper(name.Trim()[0]);
            int hash = (int)first % 5;

            return hash switch
            {
                0 => "from-indigo-500 to-violet-600",
                1 => "from-sky-500 to-blue-600",
                2 => "from-emerald-500 to-teal-600",
                3 => "from-amber-500 to-orange-600",
                _ => "from-rose-500 to-pink-600"
            };
        }
    }
}
