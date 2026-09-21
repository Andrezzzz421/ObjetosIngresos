using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using ObjetosIngresos.ViewModel;

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
            if (archivoImagen != null && archivoImagen.Length > 0)
            {
                obj.FotoArchivo = await APHelpers.ToBytes(archivoImagen);
            }

            if (detalles != null && detalles.Any())
            {
                obj.DetalleElementos = detalles.Where(d => d.IdTipoDetalle > 0).ToList();
            }

            db.Elementos.Add(obj);
            await db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Elemento obj, List<DetalleElemento> detalles, IFormFile? archivoImagen)
        {
            var elementoExistente = await db.Elementos.FindAsync(obj.IdElemento);
            if (elementoExistente == null) throw new Exception("El equipo no existe.");

            elementoExistente.TipoElemento = obj.TipoElemento;
            elementoExistente.IdMarca = obj.IdMarca;
            elementoExistente.Serial = obj.Serial;

            // Procesamiento asíncrono de la imagen solo si se adjuntó un archivo válido
            if (archivoImagen != null && archivoImagen.Length > 0)
            {
                elementoExistente.FotoArchivo = await APHelpers.ToBytes(archivoImagen);
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
        }

        public async Task DeleteAsync(int id)
        {
            var obj = await db.Elementos.FindAsync(id);
            if (obj == null) return;

            // 1. Validar si tiene algún movimiento activo (ingresado sin registrar salida)
            var tieneMovimientoActivo = await db.RegistrosMovimientos
                .AnyAsync(m => m.IdElemento == id && m.FechaSalida == null);

            if (tieneMovimientoActivo)
            {
                throw new InvalidOperationException("El equipo se encuentra actualmente dentro del establecimiento (tiene un ingreso activo). Primero debes registrar su salida antes de poder eliminarlo.");
            }

            // 2. Eliminar movimientos históricos cerrados y sus detalles asociados para evitar violación de llave foránea
            var movimientos = await db.RegistrosMovimientos.Where(m => m.IdElemento == id).ToListAsync();
            if (movimientos.Any())
            {
                var idMovimientos = movimientos.Select(m => m.IdMovimiento).ToList();
                var detallesMov = await db.MovimientoDetalles.Where(md => idMovimientos.Contains(md.IdMovimiento)).ToListAsync();
                if (detallesMov.Any())
                {
                    db.MovimientoDetalles.RemoveRange(detallesMov);
                }
                db.RegistrosMovimientos.RemoveRange(movimientos);
            }

            // 3. Eliminar accesorios del elemento
            var detalles = await db.DetalleElementos.Where(d => d.IdElemento == id).ToListAsync();
            if (detalles.Any())
            {
                db.DetalleElementos.RemoveRange(detalles);
            }

            // 4. Eliminar el elemento
            db.Elementos.Remove(obj);
            await db.SaveChangesAsync();
        }

        public async Task<bool> EliminarElementoAsync(int id)
        {
            var detalles = await db.DetalleElementos
                .Where(d => d.IdElemento == id)
                .ToListAsync();

            if (detalles.Any())
            {
                db.DetalleElementos.RemoveRange(detalles);
            }

            var elemento = await db.Elementos.FindAsync(id);
            if (elemento == null) return false;

            db.Elementos.Remove(elemento);
            await db.SaveChangesAsync();
            return true;
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

        public async Task<IEnumerable<ElementoIndexViewModel>> GetAllOptimizadoAsync()
        {
            return await db.Elementos
                .AsNoTracking()
                .Select(x => new ElementoIndexViewModel
                {
                    IdElemento = x.IdElemento,
                    TipoElemento = x.TipoElemento,
                    Serial = x.Serial,
                    TieneFoto = x.FotoArchivo != null,
                    PropietarioNombres = x.IdUsuarioNavigation != null ? x.IdUsuarioNavigation.Nombres : "",
                    PropietarioApellidos = x.IdUsuarioNavigation != null ? x.IdUsuarioNavigation.Apellidos : "",
                    PropietarioDocumento = x.IdUsuarioNavigation != null && x.IdUsuarioNavigation.Documento != null ? x.IdUsuarioNavigation.Documento : "Sin documento",
                    NombreMarca = x.IdMarcaNavigation != null ? x.IdMarcaNavigation.NombreMarca : "Sin Marca",
                    Accesorios = x.DetalleElementos
                        .Where(d => d.IdTipoDetalleNavigation != null)
                        .Select(d => d.IdTipoDetalleNavigation!.Nombre)
                        .ToList()
                })
                .ToListAsync();
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

        public async Task<byte[]> ExportarElementosAExcelAsync()
        {
            var elementosStream = db.Elementos
                .AsNoTracking()
                .Select(x => new
                {
                    x.IdElemento,
                    x.TipoElemento,
                    Propietario = x.IdUsuarioNavigation != null
                        ? x.IdUsuarioNavigation.Nombres + " " + x.IdUsuarioNavigation.Apellidos
                        : "Sin Propietario",
                    Documento = x.IdUsuarioNavigation != null && x.IdUsuarioNavigation.Documento != null
                        ? x.IdUsuarioNavigation.Documento
                        : "Sin documento",
                    Marca = x.IdMarcaNavigation != null ? x.IdMarcaNavigation.NombreMarca : "Sin Marca",
                    Serial = x.Serial,
                    Accesorios = string.Join(", ", x.DetalleElementos
                        .Where(d => d.IdTipoDetalleNavigation != null)
                        .Select(d => d.IdTipoDetalleNavigation!.Nombre))
                })
                .AsAsyncEnumerable();

            var columnas = new Dictionary<string, Func<dynamic, object?>>
            {
                { "ID", x => x.IdElemento },
                { "Tipo Equipo", x => x.TipoElemento },
                { "Propietario", x => x.Propietario },
                { "Documento", x => x.Documento },
                { "Marca", x => x.Marca },
                { "Serial", x => string.IsNullOrEmpty(x.Serial) ? "N/A" : x.Serial },
                { "Accesorios", x => string.IsNullOrEmpty(x.Accesorios) ? "Ninguno" : x.Accesorios }
            };

            return await ExcelExportHelper.ExportarAExcelAsync(elementosStream, "Inventario Global", columnas);
        }
    }
}