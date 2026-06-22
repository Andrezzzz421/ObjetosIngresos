using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class CatalogoServices
    {
            private readonly SistemaIngresoContext db;

            public CatalogoServices(SistemaIngresoContext db)
            {
                this.db = db;
            }
            public void AddMarca(Marca m)
            {
                db.Marcas.Add(m);
                db.SaveChanges();
            }

            public void UpdateMarca(Marca m)
            {
                db.Marcas.Update(m);
                db.SaveChanges();
            }

            public bool DeleteMarca(int id)
            {
                var marca = db.Marcas.Find(id);
                if (marca == null) return false;
                try
                {
                    db.Marcas.Remove(marca);
                    db.SaveChanges();
                    return true;
                }
                catch { return false; } 
            }

            public Marca? GetMarcaById(int id) => db.Marcas.Find(id);
            public List<Marca> GetAllMarcas() => db.Marcas.ToList();

        // <----------------------------------------------------------------------------->

            public void AddTipoDetalle(TiposDetalle t)
            {
                db.TiposDetalles.Add(t);
                db.SaveChanges();
            }

            public void UpdateTipoDetalle(TiposDetalle t)
            {
                db.TiposDetalles.Update(t);
                db.SaveChanges();
            }

            public bool DeleteTipoDetalle(int id)
            {
                var tipo = db.TiposDetalles.Find(id);
                if (tipo == null) return false;
                try
                {
                    db.TiposDetalles.Remove(tipo);
                    db.SaveChanges();
                    return true;
                }
                catch { return false; }
            }

            public TiposDetalle? GetTipoDetalleById(int id) => db.TiposDetalles.Find(id);
            public List<TiposDetalle> GetAllTiposDetalle() => db.TiposDetalles.ToList();
            public void AddRegional(Regionale r)
            {
                db.Regionales.Add(r);
                db.SaveChanges();
            }

        // <----------------------------------------------------------------------------->

            public Regionale? GetRegionalById(int id) => db.Regionales.Find(id);
            public List<Regionale> GetAllRegionales() => db.Regionales.ToList();

        // <----------------------------------------------------------------------------->

            public void AddCentroFormacion(CentrosFormacion c)
            {
                db.CentrosFormacions.Add(c);
                db.SaveChanges();
            }


            public CentrosFormacion? GetCentroById(int id) => db.CentrosFormacions.Find(id);

            public List<CentrosFormacion> GetAllCentros()
            {
                return db.CentrosFormacions
                    .Include(c => c.IdRegionalNavigation)
                    .ToList();
            }

        // <----------------------------------------------------------------------------->


            public void AddSede(Sede s)
            {
                db.Sedes.Add(s);
                db.SaveChanges();
            }

            public void UpdateSede(Sede s)
            {
                db.Sedes.Update(s);
                db.SaveChanges();
            }

            public bool DeleteSede(int id)
            {
                var sede = db.Sedes.Find(id);
                if (sede == null) return false;
                try
                {
                    db.Sedes.Remove(sede);
                    db.SaveChanges();
                    return true;
                }
                catch { return false; }
            }

            public Sede? GetSedeById(int id) => db.Sedes.Find(id);

            public List<Sede> GetAllSedes()
            {
                return db.Sedes
                    .Include(s => s.IdCentroNavigation)
                    .ToList();
            }

        // <----------------------------------------------------------------------------->


        public List<TiposUsuario> GetAllTiposUsuario() => db.TiposUsuarios.ToList();

        public TiposUsuario? GetTipoUsuarioById(int id) => db.TiposUsuarios.Find(id);

        public bool CreateTipoUsuario(TiposUsuario nuevoTipo)
        {
            try
            {
                db.TiposUsuarios.Add(nuevoTipo);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateTipoUsuario(TiposUsuario tipoActualizado)
        {
            try
            {
                var registroExistente = db.TiposUsuarios.Find(tipoActualizado.IdTipoUsuario);
                if (registroExistente == null) return false;

                registroExistente.Descripcion = tipoActualizado.Descripcion;

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteTipoUsuario(int id)
        {
            try
            {
                var registro = db.TiposUsuarios.Find(id);
                if (registro == null) return false;

                db.TiposUsuarios.Remove(registro);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    }

