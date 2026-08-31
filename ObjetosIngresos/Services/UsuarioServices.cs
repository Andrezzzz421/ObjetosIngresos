using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ObjetosIngresos.Models;
using System.Security.Claims;

namespace ObjetosIngresos.Services
{
    public class UsuarioServices
    {
        private readonly SistemaIngresoContext db;
        private readonly IMemoryCache cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
        private const string CacheKey = "UsuariosCache";


        public UsuarioServices(SistemaIngresoContext db,IMemoryCache cache)
        {
            this.db = db;
            this.cache = cache;
        }

        public async Task Add(Usuario u)
        {
            db.Usuarios.Add(u);
            await db.SaveChangesAsync();
            InvalidarCache();
        }

        public async Task Update(Usuario u)
        {
            db.Usuarios.Update(u);
            await db.SaveChangesAsync();
            InvalidarCache();
        }

        public async Task<bool> Delete(int id)
        {
            var usuario = await db.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            try
            {
                db.Usuarios.Remove(usuario);
                await db.SaveChangesAsync();
                InvalidarCache();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Usuario?> GetById(int id)
        {
            return await db.Usuarios
                .Include(u => u.IdTipoUsuarioNavigation)
                .Include(u => u.IdSedePrincipalNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == id);
        }

        public async Task<List<Usuario>> GetAll()
        {
            return await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                return await db.Usuarios
                    .AsNoTracking() 
                    .Include(u => u.IdTipoUsuarioNavigation)
                    .Include(u => u.IdSedePrincipalNavigation)
                    .ToListAsync(); 
            }) ?? new List<Usuario>();
        }

        private void InvalidarCache()
        {
            cache.Remove(CacheKey);
        }
    }
}
