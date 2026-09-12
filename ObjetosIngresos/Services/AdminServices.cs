using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class AdminServices
    {
        private readonly SistemaIngresoContext _db;

        public AdminServices(SistemaIngresoContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Obtiene y consolida toda la información de métricas y estadísticas para el Panel Administrativo.
        /// </summary>
        public async Task<AdminDashboardViewModel> GetDashboardDataAsync(ClaimsPrincipal user)
        {
            var viewModel = new AdminDashboardViewModel();

            // 1. Metadatos de sesión y fecha local (Colombia UTC-5)
            var colombiaNow = DateTime.UtcNow.AddHours(-5);
            var todayStartUtc = colombiaNow.Date.AddHours(5);
            var tomorrowStartUtc = todayStartUtc.AddDays(1);

            viewModel.NombreAdmin = user.FindFirst(ClaimTypes.Name)?.Value ?? "Administrador";
            viewModel.FechaHoy = colombiaNow.ToString("dddd, dd 'de' MMMM yyyy", new CultureInfo("es-CO"));

            // 2. Estadísticas y KPIs de Movimientos
            var statsMov = await _db.RegistrosMovimientos
                .AsNoTracking()
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Activos = g.Count(m => m.FechaSalida == null),
                    Hoy = g.Count(m => m.FechaEntrada >= todayStartUtc && m.FechaEntrada < tomorrowStartUtc),
                    SalidaHoy = g.Count(m => m.FechaSalida >= todayStartUtc && m.FechaSalida < tomorrowStartUtc)
                })
                .FirstOrDefaultAsync();

            int ingresoHoy = statsMov?.Hoy ?? 0;
            int salidaHoy = statsMov?.SalidaHoy ?? 0;
            int retornoHoyPct = ingresoHoy > 0 ? (int)Math.Round((double)salidaHoy / ingresoHoy * 100) : 0;

            viewModel.Kpis.TotalMovimientos = statsMov?.Total ?? 0;
            viewModel.Kpis.EquiposDentro = statsMov?.Activos ?? 0;
            viewModel.Kpis.IngresoHoy = ingresoHoy;
            viewModel.Kpis.SalidaHoy = salidaHoy;
            viewModel.Kpis.RetornoHoyPct = Math.Min(retornoHoyPct, 100);

            // 3. Estadísticas de Elementos, Accesorios e Infraestructura
            viewModel.Kpis.TotalElementos = await _db.Elementos.CountAsync();
            viewModel.Kpis.TotalAccesorios = await _db.DetalleElementos.CountAsync();
            viewModel.Kpis.ElementosSinMovimiento = await _db.Elementos.CountAsync(e => !e.RegistrosMovimientos.Any());
            viewModel.Kpis.TotalUsuarios = await _db.Usuarios.CountAsync();
            viewModel.Kpis.TotalSedes = await _db.Sedes.CountAsync();
            viewModel.Kpis.TotalCentros = await _db.CentrosFormacions.CountAsync();
            viewModel.Kpis.TotalRegionales = await _db.Regionales.CountAsync();
            viewModel.Kpis.TotalMarcas = await _db.Marcas.CountAsync();

            // 4. Distribución por Tipo de Equipo (Top 5) con cálculo de porcentajes
            var distEquiposRaw = await _db.Elementos
                .AsNoTracking()
                .GroupBy(e => string.IsNullOrWhiteSpace(e.TipoElemento) ? "Sin tipo" : e.TipoElemento)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .Take(5)
                .ToListAsync();

            viewModel.DistribucionEquipos = distEquiposRaw.Select(de => new AdminDistribucionEquipoDto
            {
                Tipo = de.Tipo,
                Cantidad = de.Cantidad,
                Porcentaje = viewModel.Kpis.TotalElementos > 0 
                    ? (int)Math.Round((double)de.Cantidad / viewModel.Kpis.TotalElementos * 100) 
                    : 0
            }).ToList();

            // 5. Distribución de Usuarios por Rol
            var rolesRaw = await _db.Usuarios
                .AsNoTracking()
                .GroupBy(u => u.IdTipoUsuarioNavigation != null ? u.IdTipoUsuarioNavigation.Descripcion : "Sin rol")
                .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .ToListAsync();

            viewModel.RolesUsuarios = rolesRaw.Select(r => new AdminRolDto
            {
                Rol = r.Rol,
                Cantidad = r.Cantidad
            }).ToList();

            // 6. Sedes más activas (Top 5) con porcentaje respecto a la máxima sede
            var sedesRaw = await _db.RegistrosMovimientos
                .AsNoTracking()
                .Where(m => m.IdSedeNavigation != null)
                .GroupBy(m => m.IdSedeNavigation!.NombreSede)
                .Select(g => new { Sede = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToListAsync();

            int maxSedeTotal = sedesRaw.Any() ? sedesRaw.First().Total : 1;

            viewModel.SedesActivas = sedesRaw.Select(s => new AdminSedeDto
            {
                Sede = s.Sede,
                Total = s.Total,
                Porcentaje = maxSedeTotal > 0 ? (int)Math.Round((double)s.Total / maxSedeTotal * 100) : 0
            }).ToList();

            // 7. Últimos 12 movimientos registrados
            var ultimosMovs = await _db.RegistrosMovimientos
                .AsNoTracking()
                .OrderByDescending(m => m.FechaEntrada)
                .Take(12)
                .Select(m => new
                {
                    m.IdMovimiento,
                    Propietario = m.IdElementoNavigation != null && m.IdElementoNavigation.IdUsuarioNavigation != null
                                   ? m.IdElementoNavigation.IdUsuarioNavigation.Nombres + " " + m.IdElementoNavigation.IdUsuarioNavigation.Apellidos
                                   : "Desconocido",
                    Documento = m.IdElementoNavigation != null && m.IdElementoNavigation.IdUsuarioNavigation != null
                                   ? m.IdElementoNavigation.IdUsuarioNavigation.Documento
                                   : "—",
                    Equipo = m.IdElementoNavigation != null ? m.IdElementoNavigation.TipoElemento : "—",
                    Serial = m.IdElementoNavigation != null && !string.IsNullOrEmpty(m.IdElementoNavigation.Serial) ? m.IdElementoNavigation.Serial : "S/N",
                    Sede = m.IdSedeNavigation != null ? m.IdSedeNavigation.NombreSede : "—",
                    m.FechaEntrada,
                    m.FechaSalida
                })
                .ToListAsync();

            viewModel.UltimosMovimientos = ultimosMovs.Select(m => new AdminMovimientoDto
            {
                Id = m.IdMovimiento,
                Propietario = m.Propietario ?? "Desconocido",
                Documento = m.Documento ?? "—",
                Equipo = m.Equipo ?? "—",
                Serial = m.Serial ?? "S/N",
                Sede = m.Sede ?? "—",
                FechaEntrada = m.FechaEntrada?.AddHours(-5).ToString("dd/MM/yyyy HH:mm") ?? "—",
                FechaSalida = m.FechaSalida?.AddHours(-5).ToString("dd/MM/yyyy HH:mm") ?? "—",
                Activo = m.FechaSalida == null
            }).ToList();

            // 8. Usuarios recientes (Top 6)
            var ultimosUsers = await _db.Usuarios
                .AsNoTracking()
                .OrderByDescending(u => u.IdUsuario)
                .Take(6)
                .Select(u => new
                {
                    Nombre = (u.Nombres + " " + u.Apellidos).Trim(),
                    Documento = u.Documento ?? "—",
                    Correo = u.Correo ?? string.Empty,
                    Rol = u.IdTipoUsuarioNavigation != null ? u.IdTipoUsuarioNavigation.Descripcion : "—",
                    Sede = u.IdSedePrincipalNavigation != null ? u.IdSedePrincipalNavigation.NombreSede : "—"
                })
                .ToListAsync();

            viewModel.UltimosUsuarios = ultimosUsers.Select(u => new AdminUsuarioDto
            {
                Nombre = string.IsNullOrEmpty(u.Nombre) ? "—" : u.Nombre,
                Inicial = AdminHelper.GetInitial(u.Nombre),
                Documento = u.Documento,
                Correo = u.Correo,
                Rol = u.Rol ?? "—",
                Sede = u.Sede ?? "—"
            }).ToList();

            return viewModel;
        }
    }
}
