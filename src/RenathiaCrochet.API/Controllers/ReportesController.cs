using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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

        private static byte[] GenerarPdfVentas(
            List<VentaDetalleDiaDto> detalle,
            VentaResumenDto? resumen,
            DateTime fechaInicio, DateTime fechaFin)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9));

                    page.Header().Element(HeaderVentas(fechaInicio, fechaFin));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Renathia Crochet — Reporte de Ventas | ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(8).Text("Detalle por día")
                            .FontSize(11).Bold().FontColor(Color.FromHex("#C96EA0"));

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70); // Fecha
                                c.RelativeColumn();   // Órdenes
                                c.RelativeColumn();   // Unidades
                                c.RelativeColumn();   // Subtotal
                                c.RelativeColumn();   // Envío
                                c.RelativeColumn();   // Ingresos
                                c.RelativeColumn();   // Promedio
                            });

                            // Header
                            static IContainer CeldaHeader(IContainer c) =>
                                c.Background(Color.FromHex("#C96EA0")).Padding(5)
                                 .AlignCenter().DefaultTextStyle(s => s.FontColor(Colors.White).Bold().FontSize(8));

                            table.Header(h =>
                            {
                                h.Cell().Element(CeldaHeader).Text("Fecha");
                                h.Cell().Element(CeldaHeader).Text("Órdenes");
                                h.Cell().Element(CeldaHeader).Text("Unidades");
                                h.Cell().Element(CeldaHeader).Text("Subtotal");
                                h.Cell().Element(CeldaHeader).Text("Envío");
                                h.Cell().Element(CeldaHeader).Text("Ingresos");
                                h.Cell().Element(CeldaHeader).Text("Promedio");
                            });

                            bool alt = false;
                            foreach (var d in detalle)
                            {
                                var bg = alt ? Color.FromHex("#FDF0F7") : Colors.White;
                                alt = !alt;

                                static IContainer CeldaFila(IContainer c, Color bg) =>
                                    c.Background(bg).Padding(4).AlignCenter();

                                table.Cell().Element(c => CeldaFila(c, bg)).Text(d.Fecha.ToString("dd/MM/yy"));
                                table.Cell().Element(c => CeldaFila(c, bg)).Text(d.TotalOrdenes.ToString());
                                table.Cell().Element(c => CeldaFila(c, bg)).Text(d.TotalUnidades.ToString());
                                table.Cell().Element(c => CeldaFila(c, bg)).Text($"${d.TotalSubtotal:N0}");
                                table.Cell().Element(c => CeldaFila(c, bg)).Text($"${d.TotalEnvio:N0}");
                                table.Cell().Element(c => CeldaFila(c, bg)).Text($"${d.TotalIngresos:N0}");
                                table.Cell().Element(c => CeldaFila(c, bg)).Text($"${d.PromedioOrden:N0}");
                            }
                        });

                        if (resumen != null)
                        {
                            col.Item().PaddingTop(14).PaddingBottom(6)
                                .Text("Resumen Global").FontSize(11).Bold().FontColor(Color.FromHex("#C96EA0"));

                            col.Item().Background(Color.FromHex("#FDF0F7")).Padding(12).Column(r =>
                            {
                                void Fila(string label, string value)
                                {
                                    r.Item().Row(row =>
                                    {
                                        row.RelativeItem().Text(label).Bold();
                                        row.RelativeItem().Text(value);
                                    });
                                    r.Item().PaddingBottom(3);
                                }
                                Fila("Total Órdenes:", resumen.TotalOrdenes.ToString());
                                Fila("Unidades Vendidas:", resumen.TotalUnidadesVendidas.ToString());
                                Fila("Total Ingresos:", $"${resumen.TotalIngresos:N2}");
                                Fila("Total Envíos:", $"${resumen.TotalEnvio:N2}");
                                Fila("Promedio por Orden:", $"${resumen.PromedioOrden:N2}");
                                Fila("Orden Máxima:", $"${resumen.OrdenMaxima:N2}");
                                Fila("Orden Mínima:", $"${resumen.OrdenMinima:N2}");
                            });
                        }
                    });
                });
            }).GeneratePdf();
        }

        private static byte[] GenerarPdfProductos(List<ProductoMasVendidoDto> productos, int top)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(t => t.FontSize(9));

                    page.Header().Column(h =>
                    {
                        h.Item().Text($"Top {top} Productos Más Vendidos")
                            .FontSize(16).Bold().FontColor(Color.FromHex("#C96EA0"));
                        h.Item().Text($"Renathia Crochet — generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);
                        h.Item().PaddingBottom(8);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Renathia Crochet | ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(25);  // #
                            c.RelativeColumn(3);   // Producto
                            c.RelativeColumn();    // Precio
                            c.RelativeColumn();    // Stock
                            c.RelativeColumn();    // Activo
                            c.RelativeColumn();    // Unidades
                            c.RelativeColumn();    // Órdenes
                            c.RelativeColumn(1.5f);// Ingresos
                        });

                        static IContainer CeldaH(IContainer c) =>
                            c.Background(Color.FromHex("#C96EA0")).Padding(5)
                             .AlignCenter().DefaultTextStyle(s => s.FontColor(Colors.White).Bold().FontSize(8));

                        table.Header(h =>
                        {
                            h.Cell().Element(CeldaH).Text("#");
                            h.Cell().Element(CeldaH).Text("Producto");
                            h.Cell().Element(CeldaH).Text("Precio Base");
                            h.Cell().Element(CeldaH).Text("Stock");
                            h.Cell().Element(CeldaH).Text("Activo");
                            h.Cell().Element(CeldaH).Text("Unidades");
                            h.Cell().Element(CeldaH).Text("Órdenes");
                            h.Cell().Element(CeldaH).Text("Ingresos");
                        });

                        bool alt = false;
                        int rank = 1;
                        foreach (var p in productos)
                        {
                            var bg = alt ? Color.FromHex("#FDF0F7") : Colors.White;
                            alt = !alt;

                            static IContainer CeldaF(IContainer c, Color bg) =>
                                c.Background(bg).Padding(4).AlignCenter();

                            table.Cell().Element(c => CeldaF(c, bg)).Text(rank++.ToString());
                            table.Cell().Element(c => CeldaF(c, bg)).AlignLeft().Text(p.Producto);
                            table.Cell().Element(c => CeldaF(c, bg)).Text($"${p.PrecioBase:N0}");
                            table.Cell().Element(c => CeldaF(c, bg)).Text(p.StockActual.ToString());
                            table.Cell().Element(c => CeldaF(c, bg)).Text(p.Activo ? "Sí" : "No");
                            table.Cell().Element(c => CeldaF(c, bg)).Text(p.TotalUnidadesVendidas.ToString());
                            table.Cell().Element(c => CeldaF(c, bg)).Text(p.TotalOrdenes.ToString());
                            table.Cell().Element(c => CeldaF(c, bg)).Text($"${p.TotalIngresosGenerados:N0}");
                        }
                    });
                });
            }).GeneratePdf();
        }

        private static Action<IContainer> HeaderVentas(DateTime inicio, DateTime fin) =>
            c => c.Column(h =>
            {
                h.Item().Text("Reporte de Ventas por Período")
                    .FontSize(16).Bold().FontColor(Color.FromHex("#C96EA0"));
                h.Item().Text($"Período: {inicio:dd/MM/yyyy} — {fin:dd/MM/yyyy}  |  Renathia Crochet — {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
                h.Item().PaddingBottom(8);
            });
    }
}
