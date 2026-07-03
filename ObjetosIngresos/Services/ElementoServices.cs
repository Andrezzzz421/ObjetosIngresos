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

        public async Task AddAsync(Elemento obj, List<DetalleElemento> detalles, IFormFile? archivoImagen)
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                if (archivoImagen != null)
                    obj.FotoArchivo = APHelpers.ToBytes(archivoImagen);

                db.Elementos.Add(obj);
                await db.SaveChangesAsync(); 

                if (detalles != null && detalles.Any())
                {
                    foreach (var detalle in detalles.Where(d => d.IdTipoDetalle > 0))
                    {
                        detalle.IdElemento = obj.IdElemento; 
                        db.DetalleElementos.Add(detalle);
                    }
                    await db.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; 
            }
        }

        public async Task UpdateAsync(Elemento obj, List<DetalleElemento> detalles, IFormFile? archivoImagen)
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var elementoExistente = await db.Elementos.FindAsync(obj.IdElemento);
                if (elementoExistente == null) throw new Exception("El equipo no existe.");

                elementoExistente.TipoElemento = obj.TipoElemento;
                elementoExistente.IdMarca = obj.IdMarca;
                elementoExistente.Serial = obj.Serial;

                if (archivoImagen != null && archivoImagen.Length > 0)
                {
                    elementoExistente.FotoArchivo = APHelpers.ToBytes(archivoImagen);
                }

                var detallesAnteriores = await db.DetalleElementos.Where(d => d.IdElemento == obj.IdElemento).ToListAsync();
                if (detallesAnteriores.Any())
                {
                    db.DetalleElementos.RemoveRange(detallesAnteriores);
                }

                if (detalles != null && detalles.Any())
                {
                    foreach (var det in detalles.Where(d => d.IdTipoDetalle > 0))
                    {
                        det.IdElemento = obj.IdElemento; 
                        det.IdDetalle = 0;               
                        db.DetalleElementos.Add(det);
                    }
                }
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var obj = await db.Elementos.FindAsync(id);
                if (obj != null)
                {
                   var detalles = await db.DetalleElementos.Where(d => d.IdElemento == id).ToListAsync();
                    db.DetalleElementos.RemoveRange(detalles);

                    db.Elementos.Remove(obj);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Elemento?> GetByIdAsync(int id)
        {
            return await db.Elementos
                .Include(x => x.IdMarcaNavigation)
                .Include(x => x.IdUsuarioNavigation)
                .Include(x => x.DetalleElementos)
                    .ThenInclude(d => d.IdTipoDetalleNavigation)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.IdElemento == id);
        }

        public async Task<IEnumerable<Elemento>> GetAllAsync()
        {
            return await db.Elementos
                .Include(x => x.IdMarcaNavigation)
                .Include(x => x.IdUsuarioNavigation)
                .Include(x => x.DetalleElementos)
                    .ThenInclude(d => d.IdTipoDetalleNavigation)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();
        }
    }
}
