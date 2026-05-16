namespace RenathiaCrochet.Application.DTOs
{
    public class DashboardResumenDto
    {
        public decimal TotalIngresosMes { get; set; }
        public int TotalPedidosMes { get; set; }
        public string ProductoMasVendido { get; set; } = "—";
        public int PedidosPendientesPago { get; set; }
    }

    public class VentasPorDiaDto
    {
        public string Fecha { get; set; } = string.Empty;  // "yyyy-MM-dd"
        public decimal TotalIngresos { get; set; }
        public int TotalPedidos { get; set; }
    }

    public class EstadoPedidoDto
    {
        public string Status { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class ProductoVendidoDashboardDto
    {
        public string Producto { get; set; } = string.Empty;
        public int TotalUnidades { get; set; }
    }
}
