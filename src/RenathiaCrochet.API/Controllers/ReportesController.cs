using ClosedXML.Excel;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Infrastructure.Data;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Endpoints de reportes descargables en Excel y PDF.
    /// Solo accesibles por Administrador (RoleId = 1).
    /// </summary>
    [ApiController]
    [Route("api/Admin/reportes")]
    [Authorize(Roles = "1")]
    public class ReportesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportesController(AppDbContext context)
        {
            _context = context;
        }

        // ─── VENTAS POR PERÍODO ──────────────────────────────────────────────

        [HttpGet("ventas/excel")]
        public async Task<IActionResult> VentasExcel(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var (detalle, resumen) = await EjecutarVentasPorPeriodo(fechaInicio, fechaFin);
            var bytes = GenerarExcelVentas(detalle, resumen, fechaInicio, fechaFin);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.xlsx");
        }

        [HttpGet("ventas/pdf")]
        public async Task<IActionResult> VentasPdf(
            [FromQuery] DateTime fechaInicio,
            [FromQuery] DateTime fechaFin)
        {
            var (detalle, resumen) = await EjecutarVentasPorPeriodo(fechaInicio, fechaFin);
            var bytes = GenerarPdfVentas(detalle, resumen, fechaInicio, fechaFin);
            return File(bytes, "application/pdf",
                $"Ventas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf");
        }

        // ─── PRODUCTOS MÁS VENDIDOS ──────────────────────────────────────────

        [HttpGet("productos/excel")]
        public async Task<IActionResult> ProductosExcel([FromQuery] int top = 10)
        {
            var productos = await EjecutarProductosMasVendidos(top);
            var bytes = GenerarExcelProductos(productos, top);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ProductosMasVendidos_Top{top}.xlsx");
        }

        [HttpGet("productos/pdf")]
        public async Task<IActionResult> ProductosPdf([FromQuery] int top = 10)
        {
            var productos = await EjecutarProductosMasVendidos(top);
            var bytes = GenerarPdfProductos(productos, top);
            return File(bytes, "application/pdf", $"ProductosMasVendidos_Top{top}.pdf");
        }

        // ─── ACCESO A DATOS (ADO.NET) ────────────────────────────────────────

        private async Task<(List<VentaDetalleDiaDto> detalle, VentaResumenDto? resumen)>
            EjecutarVentasPorPeriodo(DateTime fechaInicio, DateTime fechaFin)
        {
            var detalle = new List<VentaDetalleDiaDto>();
            VentaResumenDto? resumen = null;

            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_ReporteVentasPorPeriodo", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
            cmd.Parameters.AddWithValue("@FechaFin", fechaFin.Date);

            using var reader = await cmd.ExecuteReaderAsync();

            // Primer result set — detalle por día
            while (await reader.ReadAsync())
            {
                detalle.Add(new VentaDetalleDiaDto
                {
                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                    TotalOrdenes = reader.GetInt32(reader.GetOrdinal("TotalOrdenes")),
                    TotalUnidades = reader.GetInt32(reader.GetOrdinal("TotalUnidades")),
                    TotalSubtotal = reader.GetDecimal(reader.GetOrdinal("TotalSubtotal")),
                    TotalEnvio = reader.GetDecimal(reader.GetOrdinal("TotalEnvio")),
                    TotalIngresos = reader.GetDecimal(reader.GetOrdinal("TotalIngresos")),
                    PromedioOrden = reader.GetDecimal(reader.GetOrdinal("PromedioOrden")),
                });
            }

            // Segundo result set — resumen global
            if (await reader.NextResultAsync() && await reader.ReadAsync())
            {
                resumen = new VentaResumenDto
                {
                    TotalOrdenes = reader.GetInt32(reader.GetOrdinal("TotalOrdenes")),
                    TotalUnidadesVendidas = reader.GetInt32(reader.GetOrdinal("TotalUnidadesVendidas")),
                    TotalSubtotal = reader.GetDecimal(reader.GetOrdinal("TotalSubtotal")),
                    TotalEnvio = reader.GetDecimal(reader.GetOrdinal("TotalEnvio")),
                    TotalIngresos = reader.GetDecimal(reader.GetOrdinal("TotalIngresos")),
                    PromedioOrden = reader.GetDecimal(reader.GetOrdinal("PromedioOrden")),
                    OrdenMaxima = reader.IsDBNull(reader.GetOrdinal("OrdenMaxima")) ? 0 : reader.GetDecimal(reader.GetOrdinal("OrdenMaxima")),
                    OrdenMinima = reader.IsDBNull(reader.GetOrdinal("OrdenMinima")) ? 0 : reader.GetDecimal(reader.GetOrdinal("OrdenMinima")),
                };
            }

            return (detalle, resumen);
        }

        private async Task<List<ProductoMasVendidoDto>> EjecutarProductosMasVendidos(int top)
        {
            var result = new List<ProductoMasVendidoDto>();

            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            using var cmd = new SqlCommand("sp_ProductosMasVendidos", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@TopN", top);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new ProductoMasVendidoDto
                {
                    ProductoId = reader.GetInt32(reader.GetOrdinal("ProductoId")),
                    Producto = reader.GetString(reader.GetOrdinal("Producto")),
                    PrecioBase = reader.GetDecimal(reader.GetOrdinal("PrecioBase")),
                    StockActual = reader.GetInt32(reader.GetOrdinal("StockActual")),
                    Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                    TotalUnidadesVendidas = reader.GetInt32(reader.GetOrdinal("TotalUnidadesVendidas")),
                    TotalOrdenes = reader.GetInt32(reader.GetOrdinal("TotalOrdenes")),
                    TotalIngresosGenerados = reader.GetDecimal(reader.GetOrdinal("TotalIngresosGenerados")),
                });
            }

            return result;
        }

        // ─── GENERACIÓN EXCEL ────────────────────────────────────────────────

        private static byte[] GenerarExcelVentas(
            List<VentaDetalleDiaDto> detalle,
            VentaResumenDto? resumen,
            DateTime fechaInicio, DateTime fechaFin)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Ventas por Período");

            // Título
            ws.Cell(1, 1).Value = "Reporte de Ventas por Período";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#C96EA0");
            ws.Range(1, 1, 1, 7).Merge();

            ws.Cell(2, 1).Value = $"Período: {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Range(2, 1, 2, 7).Merge();

            // Encabezados detalle
            var headers = new[] { "Fecha", "Órdenes", "Unidades", "Subtotal", "Envío", "Ingresos", "Promedio/Orden" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(4, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C96EA0");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Filas de detalle
            int row = 5;
            foreach (var d in detalle)
            {
                ws.Cell(row, 1).Value = d.Fecha.ToString("dd/MM/yyyy");
                ws.Cell(row, 2).Value = d.TotalOrdenes;
                ws.Cell(row, 3).Value = d.TotalUnidades;
                ws.Cell(row, 4).Value = d.TotalSubtotal;
                ws.Cell(row, 5).Value = d.TotalEnvio;
                ws.Cell(row, 6).Value = d.TotalIngresos;
                ws.Cell(row, 7).Value = d.PromedioOrden;
                for (int c = 4; c <= 7; c++)
                    ws.Cell(row, c).Style.NumberFormat.Format = "$#,##0.00";
                if (row % 2 == 0)
                    ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#FDF0F7");
                row++;
            }

            // Resumen global
            if (resumen != null)
            {
                row++;
                ws.Cell(row, 1).Value = "RESUMEN GLOBAL";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#C96EA0");
                ws.Range(row, 1, row, 7).Merge();
                row++;

                void ResumenFila(string label, string value)
                {
                    ws.Cell(row, 1).Value = label;
                    ws.Cell(row, 1).Style.Font.Bold = true;
                    ws.Cell(row, 2).Value = value;
                    ws.Range(row, 2, row, 7).Merge();
                    row++;
                }

                ResumenFila("Total Órdenes:", resumen.TotalOrdenes.ToString());
                ResumenFila("Unidades Vendidas:", resumen.TotalUnidadesVendidas.ToString());
                ResumenFila("Total Ingresos:", $"${resumen.TotalIngresos:N2}");
                ResumenFila("Total Envíos:", $"${resumen.TotalEnvio:N2}");
                ResumenFila("Promedio por Orden:", $"${resumen.PromedioOrden:N2}");
                ResumenFila("Orden Máxima:", $"${resumen.OrdenMaxima:N2}");
                ResumenFila("Orden Mínima:", $"${resumen.OrdenMinima:N2}");
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static byte[] GenerarExcelProductos(List<ProductoMasVendidoDto> productos, int top)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Productos Más Vendidos");

            ws.Cell(1, 1).Value = $"Top {top} Productos Más Vendidos";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#C96EA0");
            ws.Range(1, 1, 1, 8).Merge();

            var headers = new[] { "#", "Producto", "Precio Base", "Stock", "Activo", "Unidades Vendidas", "Órdenes", "Ingresos Generados" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(3, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C96EA0");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int row = 4;
            int rank = 1;
            foreach (var p in productos)
            {
                ws.Cell(row, 1).Value = rank++;
                ws.Cell(row, 2).Value = p.Producto;
                ws.Cell(row, 3).Value = p.PrecioBase;
                ws.Cell(row, 3).Style.NumberFormat.Format = "$#,##0.00";
                ws.Cell(row, 4).Value = p.StockActual;
                ws.Cell(row, 5).Value = p.Activo ? "Sí" : "No";
                ws.Cell(row, 6).Value = p.TotalUnidadesVendidas;
                ws.Cell(row, 7).Value = p.TotalOrdenes;
                ws.Cell(row, 8).Value = p.TotalIngresosGenerados;
                ws.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                if (row % 2 == 0)
                    ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#FDF0F7");
                row++;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        // ─── GENERACIÓN PDF ──────────────────────────────────────────────────

        private static readonly DeviceRgb ColorPink = new(201, 110, 160);
        private static readonly DeviceRgb ColorPinkLight = new(253, 240, 247);

        private static Cell HeaderCell(string text) =>
            new Cell()
                .Add(new Paragraph(text).SetBold())
                .SetBackgroundColor(ColorPink)
                .SetFontColor(ColorConstants.WHITE)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(5);

        private static Cell DataCell(string text, bool alt, bool alignLeft = false) =>
            new Cell()
                .Add(new Paragraph(text))
                .SetBackgroundColor(alt ? ColorPinkLight : ColorConstants.WHITE)
                .SetFontSize(8)
                .SetTextAlignment(alignLeft ? TextAlignment.LEFT : TextAlignment.CENTER)
                .SetPadding(4);

        private static byte[] GenerarPdfVentas(
            List<VentaDetalleDiaDto> detalle,
            VentaResumenDto? resumen,
            DateTime fechaInicio, DateTime fechaFin)
        {
            var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(40, 40, 40, 40);

            // Título
            doc.Add(new Paragraph("Reporte de Ventas por Período")
                .SetFontSize(16).SetBold().SetFontColor(ColorPink).SetMarginBottom(2));
            doc.Add(new Paragraph($"Período: {fechaInicio:dd/MM/yyyy} — {fechaFin:dd/MM/yyyy}  |  Renathia Crochet — {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(10));

            // Subtítulo detalle
            doc.Add(new Paragraph("Detalle por día")
                .SetFontSize(11).SetBold().SetFontColor(ColorPink).SetMarginBottom(6));

            // Tabla detalle
            var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 2, 1, 1, 1.5f, 1.5f, 1.5f, 1.5f }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("Fecha"));
            table.AddHeaderCell(HeaderCell("Órdenes"));
            table.AddHeaderCell(HeaderCell("Unidades"));
            table.AddHeaderCell(HeaderCell("Subtotal"));
            table.AddHeaderCell(HeaderCell("Envío"));
            table.AddHeaderCell(HeaderCell("Ingresos"));
            table.AddHeaderCell(HeaderCell("Promedio"));

            bool alt = false;
            foreach (var d in detalle)
            {
                table.AddCell(DataCell(d.Fecha.ToString("dd/MM/yy"), alt));
                table.AddCell(DataCell(d.TotalOrdenes.ToString(), alt));
                table.AddCell(DataCell(d.TotalUnidades.ToString(), alt));
                table.AddCell(DataCell($"${d.TotalSubtotal:N0}", alt));
                table.AddCell(DataCell($"${d.TotalEnvio:N0}", alt));
                table.AddCell(DataCell($"${d.TotalIngresos:N0}", alt));
                table.AddCell(DataCell($"${d.PromedioOrden:N0}", alt));
                alt = !alt;
            }
            doc.Add(table);

            // Resumen global
            if (resumen != null)
            {
                doc.Add(new Paragraph("Resumen Global")
                    .SetFontSize(11).SetBold().SetFontColor(ColorPink).SetMarginTop(14).SetMarginBottom(6));

                var resumenTable = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                    .UseAllAvailableWidth()
                    .SetBackgroundColor(ColorPinkLight);

                void ResumenFila(string label, string valor)
                {
                    resumenTable.AddCell(new Cell().Add(new Paragraph(label).SetBold()).SetFontSize(9).SetPadding(5).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                    resumenTable.AddCell(new Cell().Add(new Paragraph(valor)).SetFontSize(9).SetPadding(5).SetBorder(iText.Layout.Borders.Border.NO_BORDER));
                }

                ResumenFila("Total Órdenes:", resumen.TotalOrdenes.ToString());
                ResumenFila("Unidades Vendidas:", resumen.TotalUnidadesVendidas.ToString());
                ResumenFila("Total Ingresos:", $"${resumen.TotalIngresos:N2}");
                ResumenFila("Total Envíos:", $"${resumen.TotalEnvio:N2}");
                ResumenFila("Promedio por Orden:", $"${resumen.PromedioOrden:N2}");
                ResumenFila("Orden Máxima:", $"${resumen.OrdenMaxima:N2}");
                ResumenFila("Orden Mínima:", $"${resumen.OrdenMinima:N2}");
                doc.Add(resumenTable);
            }

            doc.Close();
            return ms.ToArray();
        }

        private static byte[] GenerarPdfProductos(List<ProductoMasVendidoDto> productos, int top)
        {
            var ms = new MemoryStream();
            var writer = new PdfWriter(ms);
            var pdf = new PdfDocument(writer);
            var doc = new Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(40, 40, 40, 40);

            // Título
            doc.Add(new Paragraph($"Top {top} Productos Más Vendidos")
                .SetFontSize(16).SetBold().SetFontColor(ColorPink).SetMarginBottom(2));
            doc.Add(new Paragraph($"Renathia Crochet — generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                .SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(10));

            var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 0.5f, 3, 1.2f, 1, 1, 1.2f, 1, 1.5f }))
                .UseAllAvailableWidth();

            table.AddHeaderCell(HeaderCell("#"));
            table.AddHeaderCell(HeaderCell("Producto"));
            table.AddHeaderCell(HeaderCell("Precio Base"));
            table.AddHeaderCell(HeaderCell("Stock"));
            table.AddHeaderCell(HeaderCell("Activo"));
            table.AddHeaderCell(HeaderCell("Unidades"));
            table.AddHeaderCell(HeaderCell("Órdenes"));
            table.AddHeaderCell(HeaderCell("Ingresos"));

            bool alt = false;
            int rank = 1;
            foreach (var p in productos)
            {
                table.AddCell(DataCell(rank++.ToString(), alt));
                table.AddCell(DataCell(p.Producto, alt, alignLeft: true));
                table.AddCell(DataCell($"${p.PrecioBase:N0}", alt));
                table.AddCell(DataCell(p.StockActual.ToString(), alt));
                table.AddCell(DataCell(p.Activo ? "Sí" : "No", alt));
                table.AddCell(DataCell(p.TotalUnidadesVendidas.ToString(), alt));
                table.AddCell(DataCell(p.TotalOrdenes.ToString(), alt));
                table.AddCell(DataCell($"${p.TotalIngresosGenerados:N0}", alt));
                alt = !alt;
            }
            doc.Add(table);

            doc.Close();
            return ms.ToArray();
        }
    }
}
