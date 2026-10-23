using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using ObjetosIngresos.Models;
using System.Data;

namespace ObjetosIngresos.Services
{
    public class ImportacionServices
    {
        private readonly SistemaIngresoContext _db;

        public ImportacionServices(SistemaIngresoContext db)
        {
            _db = db;
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public async Task<(int Registrados, List<string> Errores)> ImportarUsuariosDesdeExcel(IFormFile archivo)
        {
            var errores = new List<string>();
            var nuevosUsuarios = new List<Usuario>();
            int registrados = 0;

            if (archivo == null || archivo.Length == 0)
            {
                errores.Add("El archivo subido está vacío.");
                return (0, errores);
            }

            using (var stream = archivo.OpenReadStream())
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // AsDataSet requiere el paquete NuGet ExcelDataReader.DataSet
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true // Considera la primera fila como nombres de columna
                        }
                    });

                    if (result.Tables.Count == 0)
                    {
                        errores.Add("El libro de Excel no contiene hojas de trabajo.");
                        return (0, errores);
                    }

                    var tabla = result.Tables[0];
                    int filaNumero = 1;

                    foreach (DataRow fila in tabla.Rows)
                    {
                        filaNumero++;

                        string documento = fila["Documento"]?.ToString()?.Trim() ?? "";
                        string nombres = fila["Nombres"]?.ToString()?.Trim() ?? "";
                        string apellidos = fila["Apellidos"]?.ToString()?.Trim() ?? "";
                        string correo = fila["Correo"]?.ToString()?.Trim()?.ToLower() ?? "";
                        string ficha = fila["Ficha"]?.ToString()?.Trim() ?? "";

                        // Parseo de IDs obligatorios/opcionales
                        int.TryParse(fila["IdTipoUsuario"]?.ToString(), out int idTipoUsuario);
                        int? idSedePrincipal = int.TryParse(fila["IdSedePrincipal"]?.ToString(), out int sedeParsed) ? sedeParsed : null;

                        // Validaciones básicas de campos requeridos
                        if (string.IsNullOrEmpty(documento) || string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(nombres))
                        {
                            errores.Add($"Fila {filaNumero}: Los campos Documento, Nombres y Correo son obligatorios.");
                            continue;
                        }

                        if (idTipoUsuario <= 0)
                        {
                            errores.Add($"Fila {filaNumero}: El 'IdTipoUsuario' proporcionado no es válido.");
                            continue;
                        }

                        // Verificación contra duplicados en la BD
                        bool existe = await _db.Usuarios.AnyAsync(u => u.Documento == documento || u.Correo == correo);
                        if (existe)
                        {
                            errores.Add($"Fila {filaNumero}: El usuario con documento {documento} o correo {correo} ya existe en el sistema.");
                            continue;
                        }

                        // Asignación ajustada a tu modelo exacto
                        var usuario = new Usuario
                        {
                            Documento = documento,
                            Nombres = nombres,
                            Apellidos = apellidos,
                            Correo = correo,
                            Ficha = string.IsNullOrWhiteSpace(ficha) ? null : ficha,
                            IdTipoUsuario = idTipoUsuario,
                            IdSedePrincipal = idSedePrincipal
                        };

                        nuevosUsuarios.Add(usuario);
                    }
                }
            }

            if (nuevosUsuarios.Count > 0)
            {
                await _db.Usuarios.AddRangeAsync(nuevosUsuarios);
                registrados = await _db.SaveChangesAsync();
            }

            return (registrados, errores);
        }
    }
}