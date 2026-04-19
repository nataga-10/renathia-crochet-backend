using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.Services;
using System.Security.Claims;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador para consultar el historial de pedidos.
    /// Implementa HU-09 (Ver estado del pedido).
    /// El usuario solo puede ver SUS propios pedidos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(claim!.Value);
        }

        /// <summary>
        /// GET api/Orders
        /// HU-09: Retorna todos los pedidos del usuario autenticado.
        /// No incluye el carrito actual (Status = PendingPayment).
        /// Incluye el historial de tracking de cada pedido.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                var userId = GetUserId();
                var orders = await _orderService.GetMyOrdersAsync(userId);

                if (!orders.Any())
                    return Ok(new { message = "No tienes pedidos aun" });

                return Ok(orders);
            }
            catch
            {
                return Unauthorized(new { message = "Debes iniciar sesion para ver tus pedidos" });
            }
        }

        /// <summary>
        /// GET api/Orders/{orderId}
        /// Retorna el detalle de un pedido especifico.
        /// Solo el dueno del pedido puede verlo.
        /// </summary>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                var userId = GetUserId();
                var order = await _orderService.GetOrderByIdAsync(orderId, userId);

                if (order == null)
                    return NotFound(new { message = "Pedido no encontrado" });

                return Ok(order);
            }
            catch
            {
                return Unauthorized(new { message = "Debes iniciar sesion para ver tus pedidos" });
            }
        }
    }
}