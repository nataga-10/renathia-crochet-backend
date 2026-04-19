using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;
using System.Security.Claims;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador del carrito de compras.
    /// Todos los endpoints requieren que el usuario este autenticado (JWT).
    /// El UserId se obtiene del token JWT, no del body de la peticion.
    /// Implementa HU-05, HU-06, HU-07 y HU-08.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Obtiene el UserId del token JWT del usuario autenticado.
        /// Es mas seguro que recibirlo como parametro porque no se puede falsificar.
        /// </summary>
        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(claim!.Value);
        }

        /// <summary>
        /// GET api/Cart
        /// HU-05: Obtiene el carrito actual del usuario.
        /// Retorna los productos, cantidades y totales.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.GetCartAsync(userId);
                return Ok(cart);
            }
            catch
            {
                return Unauthorized(new { message = "Debes iniciar sesion para ver tu carrito" });
            }
        }

        /// <summary>
        /// POST api/Cart
        /// HU-05: Agrega un producto al carrito.
        /// Si el carrito no existe lo crea automaticamente.
        /// Si el producto ya esta en el carrito aumenta la cantidad.
        /// Body: { productId, quantity, productColorId (opcional) }
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.AddToCartAsync(userId, dto);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// PUT api/Cart/items/{orderItemId}
        /// HU-06: Actualiza la cantidad de un producto en el carrito.
        /// Si quantity = 0, elimina el producto del carrito.
        /// Body: { quantity }
        /// </summary>
        [HttpPut("items/{orderItemId}")]
        public async Task<IActionResult> UpdateCartItem(int orderItemId, [FromBody] UpdateCartItemDto dto)
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.UpdateCartItemAsync(userId, orderItemId, dto);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE api/Cart/items/{orderItemId}
        /// HU-07: Elimina un producto del carrito.
        /// </summary>
        [HttpDelete("items/{orderItemId}")]
        public async Task<IActionResult> RemoveFromCart(int orderItemId)
        {
            try
            {
                var userId = GetUserId();
                var cart = await _cartService.RemoveFromCartAsync(userId, orderItemId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// POST api/Cart/checkout
        /// HU-08: Finaliza la compra.
        /// Cambia el estado del carrito a PaymentReceived.
        /// Body: { deliveryMethod, shippingAddressId (opcional), notes (opcional) }
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
        {
            try
            {
                var userId = GetUserId();
                var order = await _cartService.CheckoutAsync(userId, dto);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}