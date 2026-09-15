using ClosedXML.Excel;
using Microsoft.Build.Tasks;
using ObjetosIngresos.Helpers;
using ObjetosIngresos.Models;
using System.Runtime.Intrinsics.Arm;

namespace ObjetosIngresos.Helpers
{
    public static class ExcelExportHelper
    {
        /// <summary>
        /// Genera un archivo Excel en formato byte array recibiendo un flujo IAsyncEnumerable para alta eficiencia en memoria.
        /// </summary>
        public static async Task<byte[]> ExportarAExcelAsync<T>(
            IAsyncEnumerable<T> datosStream,
            string nombreHoja,
            Dictionary<string, Func<T, object?>> columnas)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(nombreHoja);

            // 1. Dibujar Encabezados
            int colIndex = 1;
            foreach (var columna in columnas.Keys)
            {
                var cell = worksheet.Cell(1, colIndex);
                cell.Value = columna;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A"); // Slate 900
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                colIndex++;
            }

            int rowIndex = 2;

            await foreach (var item in datosStream)
            {
                colIndex = 1;
                foreach (var extractor in columnas.Values)
                {
                    var valor = extractor(item);

                    if (valor != null)
                    {
                        worksheet.Cell(rowIndex, colIndex).Value = XLCellValue.FromObject(valor);
                    }
                    else
                    {
                        worksheet.Cell(rowIndex, colIndex).Value = "N/A";
                    }

                    colIndex++;
                }
                rowIndex++;
            }

            if (rowIndex > 2)
            {
                var totalColumnas = columnas.Count;
                var dataRange = worksheet.Range(1, 1, rowIndex - 1, totalColumnas);

                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                dataRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");

                worksheet.Columns(1, totalColumnas).AdjustToContents();
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}