using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;

namespace ObjetosIngresos.Services
{
    public class ElementoServices
    {
        private readonly SistemaIngresoContext db;

        public ElementoServices(SistemaIngresoContext db)
        {
            this.db = db;
        }

        public void Add(Elemento obj, List<DetalleElemento> detalles, IFormFile? archivoImagen)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                if (archivoImagen != null)
                    obj.FotoArchivo = APHelpers.ToBytes(archivoImagen);

                db.Elementos.Add(obj);
                db.SaveChanges(); 

                if (detalles != null && detalles.Any())
                {
                    foreach (var detalle in detalles)
                    {
                        detalle.IdElemento = obj.IdElemento; 
                        db.DetalleElementos.Add(detalle);
                    }
                    db.SaveChanges();
                }

                transaction.Commit();
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw; 
            }
        }

        public void Update(Elemento obj, List<DetalleElemento> detalles, IFormFile? archivoImagen)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var elementoExistente = db.Elementos.Find(obj.IdElemento);
                if (elementoExistente == null) throw new Exception("El equipo no existe.");

                elementoExistente.TipoElemento = obj.TipoElemento;
                elementoExistente.IdMarca = obj.IdMarca;
                elementoExistente.Serial = obj.Serial;

                if (archivoImagen != null && archivoImagen.Length > 0)
                {
                    elementoExistente.FotoArchivo = APHelpers.ToBytes(archivoImagen);
                }

                var detallesAnteriores = db.DetalleElementos.Where(d => d.IdElemento == obj.IdElemento).ToList();
                if (detallesAnteriores.Any())
                {
                    db.DetalleElementos.RemoveRange(detallesAnteriores);
                }

                if (detalles != null && detalles.Any())
                {
                    foreach (var det in detalles)
                    {
                        det.IdElemento = obj.IdElemento; 
                        det.IdDetalle = 0;               
                        db.DetalleElementos.Add(det);
                    }
                }
                db.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw;
            }
        }
        public void Delete(int id)
        {
            using var transaction = db.Database.BeginTransaction();
            try
            {
                var obj = db.Elementos.Find(id);
                if (obj != null)
                {
                   var detalles = db.DetalleElementos.Where(d => d.IdElemento == id);
                    db.DetalleElementos.RemoveRange(detalles);

                    db.Elementos.Remove(obj);
                    db.SaveChanges();
                    transaction.Commit();
                }
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
       public Elemento? GetById(int id)
        {
            return db.Elementos
                .Include(x => x.IdMarcaNavigation)
                .Include(x => x.IdUsuarioNavigation)
                .Include(x => x.DetalleElementos)
                    .ThenInclude(d => d.IdTipoDetalleNavigation)
                .FirstOrDefault(x => x.IdElemento == id);
        }

        public IEnumerable<Elemento?>? GetAll()
        {
            return db.Elementos
                .Include(x => x.IdMarcaNavigation)
                .Include(x => x.IdUsuarioNavigation)
                .Include(x => x.DetalleElementos)
                    .ThenInclude(d => d.IdTipoDetalleNavigation)
                .ToList();
        }
    }
}
