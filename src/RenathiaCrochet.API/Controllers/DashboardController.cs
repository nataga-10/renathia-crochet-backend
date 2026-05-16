using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Infrastructure.Data;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Datos para el dashboard del administrador.
    /// Todos los endpoints requieren rol Admin (RoleId = 1).
    /// </summary>
    [ApiController]
    [Route("api/Admin/dashboard")]
    [Authorize(Roles = "1")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        private async Task<SqlConnection> AbrirConexion()
        {
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();
            return conn;
        }

        /// <summary>
        /// Tarjetas de resumen: ingresos del mes, pedidos del mes,
        /// producto más vendido y pedidos pendientes de pago.
        /// </summary>
        [HttpGet("resumen")]
        public async Task<IActionResult> Resumen()
        {
            var conn = await AbrirConexion();
            var dto = new DashboardResumenDto();

            // Ingresos y pedidos del mes actual (excluye Cancelled y PendingPayment)
            const string sqlMes = @"
                SELECT
                    ISNULL(SUM(o.Total), 0)      AS TotalIngresos,
                    COUNT(DISTINCT o.OrderId)     AS TotalPedidos
                FROM Orders o
                WHERE MONTH(o.CreatedAt) = MONTH(GETUTCDATE())
                  AND YEAR(o.CreatedAt)  = YEAR(GETUTCDATE())
                  AND o.Status NOT IN ('Cancelled', 'PendingPayment')";

            using (var cmd = new SqlCommand(sqlMes, conn))
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    dto.TotalIngresosMes = r.GetDecimal(r.GetOrdinal("TotalIngresos"));
                    dto.TotalPedidosMes  = r.GetInt32(r.GetOrdinal("TotalPedidos"));
                }
            }

            // Producto más vendido del mes
            const string sqlTop = @"
                SELECT TOP 1 p.Name
                FROM Products p
                INNER JOIN OrderItems oi ON oi.ProductId = p.ProductId
                INNER JOIN Orders o     ON o.OrderId     = oi.OrderId
                WHERE MONTH(o.CreatedAt) = MONTH(GETUTCDATE())
                  AND YEAR(o.CreatedAt)  = YEAR(GETUTCDATE())
                  AND o.Status NOT IN ('Cancelled', 'PendingPayment')
                GROUP BY p.ProductId, p.Name
                ORDER BY SUM(oi.Quantity) DESC";

            using (var cmd = new SqlCommand(sqlTop, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result is string name) dto.ProductoMasVendido = name;
            }

            // Pedidos pendientes de pago (todos los tiempos, no solo el mes)
            const string sqlPendientes = @"
                SELECT COUNT(*) FROM Orders WHERE Status = 'PendingPayment'";

            using (var cmd = new SqlCommand(sqlPendientes, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                dto.PedidosPendientesPago = Convert.ToInt32(result);
            }

            return Ok(dto);
        }

        /// <summary>
        /// Ingresos diarios de los últimos 30 días (para el gráfico de línea).
        /// </summary>
        [HttpGet("ventas-por-dia")]
        public async Task<IActionResult> VentasPorDia()
        {
            var conn = await AbrirConexion();
            var lista = new List<VentasPorDiaDto>();

            const string sql = @"
                SELECT
                    CONVERT(VARCHAR(10), CAST(o.CreatedAt AS DATE), 120) AS Fecha,
                    SUM(o.Total)              AS TotalIngresos,
                    COUNT(DISTINCT o.OrderId) AS TotalPedidos
                FROM Orders o
                WHERE o.CreatedAt >= DATEADD(DAY, -30, GETUTCDATE())
                  AND o.Status NOT IN ('Cancelled', 'PendingPayment')
                GROUP BY CAST(o.CreatedAt AS DATE)
                ORDER BY Fecha";

            using var cmd = new SqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new VentasPorDiaDto
                {
                    Fecha         = r.GetString(r.GetOrdinal("Fecha")),
                    TotalIngresos = r.GetDecimal(r.GetOrdinal("TotalIngresos")),
                    TotalPedidos  = r.GetInt32(r.GetOrdinal("TotalPedidos")),
                });
            }

            return Ok(lista);
        }

        /// <summary>
        /// Conteo de pedidos agrupados por estado (para el pie chart).
        /// </summary>
        [HttpGet("estados-pedidos")]
        public async Task<IActionResult> EstadosPedidos()
        {
            var conn = await AbrirConexion();
            var lista = new List<EstadoPedidoDto>();

            const string sql = @"
                SELECT Status, COUNT(*) AS Total
                FROM Orders
                GROUP BY Status
                ORDER BY Total DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new EstadoPedidoDto
                {
                    Status = r.GetString(r.GetOrdinal("Status")),
                    Total  = r.GetInt32(r.GetOrdinal("Total")),
                });
            }

            return Ok(lista);
        }

        /// <summary>
        /// Top 8 productos más vendidos por unidades (para el bar chart).
        /// </summary>
        [HttpGet("productos-vendidos")]
        public async Task<IActionResult> ProductosVendidos()
        {
            var conn = await AbrirConexion();
            var lista = new List<ProductoVendidoDashboardDto>();

            const string sql = @"
                SELECT TOP 8
                    p.Name               AS Producto,
                    SUM(oi.Quantity)     AS TotalUnidades
                FROM Products p
                INNER JOIN OrderItems oi ON oi.ProductId = p.ProductId
                INNER JOIN Orders o      ON o.OrderId    = oi.OrderId
                WHERE o.Status NOT IN ('Cancelled', 'PendingPayment')
                GROUP BY p.ProductId, p.Name
                ORDER BY TotalUnidades DESC";

            using var cmd = new SqlCommand(sql, conn);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ProductoVendidoDashboardDto
                {
                    Producto      = r.GetString(r.GetOrdinal("Producto")),
                    TotalUnidades = r.GetInt32(r.GetOrdinal("TotalUnidades")),
                });
            }

            return Ok(lista);
        }
    }
}
