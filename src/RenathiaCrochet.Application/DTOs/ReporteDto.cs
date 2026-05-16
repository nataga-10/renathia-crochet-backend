namespace RenathiaCrochet.Application.DTOs
{
    /// <summary>Una fila del detalle diario de sp_ReporteVentasPorPeriodo.</summary>
    public class VentaDetalleDiaDto
    {
        public DateTime Fecha { get; set; }
        public int TotalOrdenes { get; set; }
        public int TotalUnidades { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal PromedioOrden { get; set; }
    }

    /// <summary>Resumen global del período de sp_ReporteVentasPorPeriodo.</summary>
    public class VentaResumenDto
    {
        public int TotalOrdenes { get; set; }
        public int TotalUnidadesVendidas { get; set; }
        public decimal TotalSubtotal { get; set; }
        public decimal TotalEnvio { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal PromedioOrden { get; set; }
        public decimal OrdenMaxima { get; set; }
        public decimal OrdenMinima { get; set; }
    }

    /// <summary>Una fila de sp_ProductosMasVendidos.</summary>
    public class ProductoMasVendidoDto
    {
        public int ProductoId { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal PrecioBase { get; set; }
        public int StockActual { get; set; }
        public bool Activo { get; set; }
        public int TotalUnidadesVendidas { get; set; }
        public int TotalOrdenes { get; set; }
        public decimal TotalIngresosGenerados { get; set; }
    }
}
