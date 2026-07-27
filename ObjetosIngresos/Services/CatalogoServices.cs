using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class CatalogoServices
    {
        private readonly SistemaIngresoContext db;
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

        public CatalogoServices(SistemaIngresoContext db, IMemoryCache cache)
        {
            this.db = db;
            this._cache = cache;
        }

        public async Task AddMarcaAsync(Marca m)
        {
            db.Marcas.Add(m);
            await db.SaveChangesAsync();
            _cache.Remove("MarcasCache");
        }

        public async Task UpdateMarcaAsync(Marca m)
        {
            db.Marcas.Update(m);
            await db.SaveChangesAsync();
            _cache.Remove("MarcasCache");
        }

        public async Task<bool> DeleteMarcaAsync(int id)
        {
            var marca = await db.Marcas.FindAsync(id);
            if (marca == null) return false;
            try
            {
                db.Marcas.Remove(marca);
                await db.SaveChangesAsync();
                _cache.Remove("MarcasCache");
                return true;
            }
            catch { return false; } 
        }

        public async Task<Marca?> GetMarcaByIdAsync(int id) => await db.Marcas.FindAsync(id);

        public async Task<List<Marca>> GetAllMarcasAsync()
        {
            if (!_cache.TryGetValue("MarcasCache", out List<Marca>? marcas))
            {
                marcas = await db.Marcas.AsNoTracking().ToListAsync();
                _cache.Set("MarcasCache", marcas, CacheDuration);
            }
            return marcas!;
        }

        // <----------------------------------------------------------------------------->

        public async Task AddTipoDetalleAsync(TiposDetalle t)
        {
            db.TiposDetalles.Add(t);
            await db.SaveChangesAsync();
            _cache.Remove("TiposDetalleCache");
        }

        public async Task UpdateTipoDetalleAsync(TiposDetalle t)
        {
            db.TiposDetalles.Update(t);
            await db.SaveChangesAsync();
            _cache.Remove("TiposDetalleCache");
        }

        public async Task<bool> DeleteTipoDetalleAsync(int id)
        {
            var tipo = await db.TiposDetalles.FindAsync(id);
            if (tipo == null) return false;
            try
            {
                db.TiposDetalles.Remove(tipo);
                await db.SaveChangesAsync();
                _cache.Remove("TiposDetalleCache");
                return true;
            }
            catch { return false; }
        }

        public async Task<TiposDetalle?> GetTipoDetalleByIdAsync(int id) => await db.TiposDetalles.FindAsync(id);

        public async Task<List<TiposDetalle>> GetAllTiposDetalleAsync()
        {
            if (!_cache.TryGetValue("TiposDetalleCache", out List<TiposDetalle>? tipos))
            {
                tipos = await db.TiposDetalles.AsNoTracking().ToListAsync();
                _cache.Set("TiposDetalleCache", tipos, CacheDuration);
            }
            return tipos!;
        }

        public async Task AddRegionalAsync(Regionale r)
        {
            db.Regionales.Add(r);
            await db.SaveChangesAsync();
            _cache.Remove("RegionalesCache");
        }

        // <----------------------------------------------------------------------------->

        public async Task<Regionale?> GetRegionalByIdAsync(int id) => await db.Regionales.FindAsync(id);

        public async Task<List<Regionale>> GetAllRegionalesAsync()
        {
            if (!_cache.TryGetValue("RegionalesCache", out List<Regionale>? regionales))
            {
                regionales = await db.Regionales.AsNoTracking().ToListAsync();
                _cache.Set("RegionalesCache", regionales, CacheDuration);
            }
            return regionales!;
        }

        // <----------------------------------------------------------------------------->

        public async Task AddCentroFormacionAsync(CentrosFormacion c)
        {
            db.CentrosFormacions.Add(c);
            await db.SaveChangesAsync();
            _cache.Remove("CentrosCache");
        }

        public async Task<CentrosFormacion?> GetCentroByIdAsync(int id) => await db.CentrosFormacions.FindAsync(id);

        public async Task<List<CentrosFormacion>> GetAllCentrosAsync()
        {
            if (!_cache.TryGetValue("CentrosCache", out List<CentrosFormacion>? centros))
            {
                centros = await db.CentrosFormacions
                    .Include(c => c.IdRegionalNavigation)
                    .AsNoTracking()
                    .ToListAsync();
                _cache.Set("CentrosCache", centros, CacheDuration);
            }
            return centros!;
        }

        // <----------------------------------------------------------------------------->

        public async Task AddSedeAsync(Sede s)
        {
            db.Sedes.Add(s);
            await db.SaveChangesAsync();
            _cache.Remove("SedesCache");
        }

        public async Task UpdateSedeAsync(Sede s)
        {
            db.Sedes.Update(s);
            await db.SaveChangesAsync();
            _cache.Remove("SedesCache");
        }

        public async Task<bool> DeleteSedeAsync(int id)
        {
            var sede = await db.Sedes.FindAsync(id);
            if (sede == null) return false;
            try
            {
                db.Sedes.Remove(sede);
                await db.SaveChangesAsync();
                _cache.Remove("SedesCache");
                return true;
            }
            catch { return false; }
        }

        public async Task<Sede?> GetSedeByIdAsync(int id) => await db.Sedes.FindAsync(id);

        public async Task<List<Sede>> GetAllSedesAsync()
        {
            if (!_cache.TryGetValue("SedesCache", out List<Sede>? sedes))
            {
                sedes = await db.Sedes
                    .Include(s => s.IdCentroNavigation)
                    .AsNoTracking()
                    .ToListAsync();
                _cache.Set("SedesCache", sedes, CacheDuration);
            }
            return sedes!;
        }

        // <----------------------------------------------------------------------------->

        public async Task<List<TiposUsuario>> GetAllTiposUsuarioAsync()
        {
            if (!_cache.TryGetValue("TiposUsuarioCache", out List<TiposUsuario>? tipos))
            {
                tipos = await db.TiposUsuarios.AsNoTracking().ToListAsync();
                _cache.Set("TiposUsuarioCache", tipos, CacheDuration);
            }
            return tipos!;
        }

        public async Task<TiposUsuario?> GetTipoUsuarioByIdAsync(int id) => await db.TiposUsuarios.FindAsync(id);

        public async Task<bool> CreateTipoUsuarioAsync(TiposUsuario nuevoTipo)
        {
            try
            {
                db.TiposUsuarios.Add(nuevoTipo);
                await db.SaveChangesAsync();
                _cache.Remove("TiposUsuarioCache");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTipoUsuarioAsync(TiposUsuario tipoActualizado)
        {
            try
            {
                var registroExistente = await db.TiposUsuarios.FindAsync(tipoActualizado.IdTipoUsuario);
                if (registroExistente == null) return false;

                registroExistente.Descripcion = tipoActualizado.Descripcion;

                await db.SaveChangesAsync();
                _cache.Remove("TiposUsuarioCache");
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTipoUsuarioAsync(int id)
        {
            try
            {
                var registro = await db.TiposUsuarios.FindAsync(id);
                if (registro == null) return false;

                db.TiposUsuarios.Remove(registro);
                await db.SaveChangesAsync();
                _cache.Remove("TiposUsuarioCache");
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

