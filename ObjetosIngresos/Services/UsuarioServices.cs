using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using System.Security.Claims;

namespace ObjetosIngresos.Services
{
    public class UsuarioServices
    {
        private readonly SistemaIngresoContext db;

        public UsuarioServices(SistemaIngresoContext db)
        {
            this.db = db;
        }
        public async Task<bool> ExisteDocumento(string documento, int idUsuarioActual = 0)
        {
            if (string.IsNullOrWhiteSpace(documento)) return false;
            var docLimpio = documento.Trim();

            return await db.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Documento == docLimpio && u.IdUsuario != idUsuarioActual);
        }
        public async Task<bool> ExisteCorreo(string correo, int idUsuarioActual = 0)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            var correoLimpio = correo.Trim().ToLower();

            return await db.Usuarios
                .AsNoTracking()
                .AnyAsync(u => u.Correo.ToLower() == correoLimpio && u.IdUsuario != idUsuarioActual);
        }
        public async Task Add(Usuario u)
        {
            u.Documento = u.Documento?.Trim();
            u.Correo = u.Correo?.Trim().ToLower();

            if (await ExisteDocumento(u.Documento))
            {
                throw new InvalidOperationException($"El número de documento '{u.Documento}' ya se encuentra registrado.");
            }

            if (await ExisteCorreo(u.Correo))
            {
                throw new InvalidOperationException($"El correo electrónico '{u.Correo}' ya se encuentra registrado.");
            }

            db.Usuarios.Add(u);
            await db.SaveChangesAsync();
        }

        public async Task Update(Usuario u)
        {
            u.Documento = u.Documento?.Trim();
            u.Correo = u.Correo?.Trim().ToLower();

            if (await ExisteDocumento(u.Documento, u.IdUsuario))
            {
                throw new InvalidOperationException($"El número de documento '{u.Documento}' ya pertenece a otro usuario.");
            }

            if (await ExisteCorreo(u.Correo, u.IdUsuario))
            {
                throw new InvalidOperationException($"El correo electrónico '{u.Correo}' ya pertenece a otro usuario.");
            }

            db.Usuarios.Update(u);
            await db.SaveChangesAsync();
        }

        public bool Delete(int id)
        {
            var usuario = db.Usuarios.Find(id);
            if (usuario == null) return false;

            try
            {
                db.Usuarios.Remove(usuario);
                db.SaveChanges();
                return true; 
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Usuario? GetById(int id)
        {
            return db.Usuarios.Find(id);
        }

        public async Task<List<Usuario>> GetAllAsync()
        {
            return await db.Usuarios
                .AsNoTracking()
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .ToListAsync();
        }

        public List<Usuario> GetAll()
        {
            return db.Usuarios
                .AsNoTracking()
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .ToList();
        }

        public async Task<byte[]> ExportarUsuariosAExcelAsync()
        {
            var usuariosStream = db.Usuarios
                .AsNoTracking()
                .Select(u => new
                {
                    u.IdUsuario,
                    Documento = u.Documento ?? "Sin documento",
                    NombreCompleto = $"{u.Nombres} {u.Apellidos}".Trim(),
                    Correo = u.Correo,
                    Ficha = u.Ficha ?? "N/A",
                    TipoUsuario = u.IdTipoUsuarioNavigation != null
                        ? u.IdTipoUsuarioNavigation.Descripcion
                        : "Sin Tipo",
                    SedePrincipal = u.IdSedePrincipalNavigation != null
                        ? u.IdSedePrincipalNavigation.NombreSede
                        : "Sin Sede"
                })
                .AsAsyncEnumerable();

            var columnas = new Dictionary<string, Func<dynamic, object?>>
            {
                { "ID", u => u.IdUsuario },
                { "Documento", u => u.Documento },
                { "Nombre Completo", u => u.NombreCompleto },
                { "Correo Electrónico", u => u.Correo },
                { "Ficha", u => u.Ficha },
                { "Tipo de Usuario", u => u.TipoUsuario },
                { "Sede Principal", u => u.SedePrincipal }
            };

            return await ExcelExportHelper.ExportarAExcelAsync(usuariosStream, "Directorio de Usuarios", columnas);
        }
    }
}
