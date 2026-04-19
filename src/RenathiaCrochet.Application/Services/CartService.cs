using System;
using System.Collections.Generic;
using System.Text;

using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Servicio que maneja toda la logica del carrito de compras.
    /// El carrito es una Order con Status = "PendingPayment".
    /// Implementa HU-05, HU-06, HU-07 y HU-08.
    /// </summary>
    public class CartService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public CartService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        /// <summary>
        /// HU-05: Agrega un producto al carrito del usuario.
        /// Si el usuario no tiene carrito, crea uno nuevo.
        /// Si el producto ya esta en el carrito, aumenta la cantidad.
        /// Si el producto es nuevo en el carrito, crea un nuevo OrderItem.
        /// </summary>
        public async Task<CartDto> AddToCartAsync(int userId, AddToCartDto dto)
        {
            // 1. Verificar que el producto existe y esta activo
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null || !product.IsActive)
                throw new Exception("Producto no disponible");

            // 2. Buscar si el usuario ya tiene un carrito activo
            var cart = await _orderRepository.GetCartByUserIdAsync(userId);

            // 3. Si no tiene carrito, crear uno nuevo
            if (cart == null)
            {
                cart = new Order
                {
                    UserId = userId,
                    Status = "PendingPayment",
                    Subtotal = 0,
                    Total = 0,
                    DeliveryMethod = "Shipping"
                };
                await _orderRepository.AddAsync(cart);

                // Agregar tracking inicial
                await _orderRepository.AddTrackingAsync(new OrderTracking
                {
                    OrderId = cart.OrderId,
                    Status = "PendingPayment",
                    Notes = "Carrito creado"
                });
            }

            // 4. Verificar si el producto ya esta en el carrito
            var existingItem = cart.Items
                .FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                // 5a. Si ya existe, aumentar la cantidad
                existingItem.Quantity += dto.Quantity;
                await _orderRepository.UpdateItemAsync(existingItem);
            }
            else
            {
                // 5b. Si es nuevo, agregar el item al carrito
                var newItem = new OrderItem
                {
                    OrderId = cart.OrderId,
                    ProductId = dto.ProductId,
                    ProductColorId = dto.ProductColorId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.BasePrice
                };
                await _orderRepository.AddItemAsync(newItem);
            }

            // 6. Recalcular totales del carrito
            await RecalcularTotalesAsync(cart);

            // 7. Retornar el carrito actualizado
            var cartActualizado = await _orderRepository.GetCartByUserIdAsync(userId);
            return MapToCartDto(cartActualizado!);
        }

        /// <summary>
        /// Obtiene el carrito actual del usuario.
        /// Si no tiene carrito retorna un carrito vacio.
        /// </summary>
        public async Task<CartDto> GetCartAsync(int userId)
        {
            var cart = await _orderRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                return new CartDto();
            return MapToCartDto(cart);
        }

        /// <summary>
        /// HU-06: Actualiza la cantidad de un producto en el carrito.
        /// Si la cantidad es 0 o menor, elimina el producto del carrito.
        /// </summary>
        public async Task<CartDto> UpdateCartItemAsync(int userId, int orderItemId, UpdateCartItemDto dto)
        {
            var cart = await _orderRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                throw new Exception("No tienes un carrito activo");

            var item = cart.Items.FirstOrDefault(i => i.OrderItemId == orderItemId);
            if (item == null)
                throw new Exception("Producto no encontrado en el carrito");

            if (dto.Quantity <= 0)
            {
                // Si cantidad es 0, eliminar el producto
                await _orderRepository.RemoveItemAsync(orderItemId);
            }
            else
            {
                // Actualizar la cantidad
                item.Quantity = dto.Quantity;
                await _orderRepository.UpdateItemAsync(item);
            }

            // Recalcular totales
            await RecalcularTotalesAsync(cart);

            var cartActualizado = await _orderRepository.GetCartByUserIdAsync(userId);
            return cartActualizado != null ? MapToCartDto(cartActualizado) : new CartDto();
        }

        /// <summary>
        /// HU-07: Elimina un producto del carrito.
        /// </summary>
        public async Task<CartDto> RemoveFromCartAsync(int userId, int orderItemId)
        {
            var cart = await _orderRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                throw new Exception("No tienes un carrito activo");

            var item = cart.Items.FirstOrDefault(i => i.OrderItemId == orderItemId);
            if (item == null)
                throw new Exception("Producto no encontrado en el carrito");

            await _orderRepository.RemoveItemAsync(orderItemId);
            await RecalcularTotalesAsync(cart);

            var cartActualizado = await _orderRepository.GetCartByUserIdAsync(userId);
            return cartActualizado != null ? MapToCartDto(cartActualizado) : new CartDto();
        }

        /// <summary>
        /// HU-08: Finaliza la compra.
        /// Cambia el estado del carrito de PendingPayment a PaymentReceived.
        /// Registra el metodo de entrega y agrega tracking.
        /// </summary>
        public async Task<OrderDto> CheckoutAsync(int userId, CheckoutDto dto)
        {
            var cart = await _orderRepository.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any())
                throw new Exception("No tienes productos en el carrito");

            // Actualizar datos de entrega
            cart.DeliveryMethod = dto.DeliveryMethod;
            cart.ShippingAddressId = dto.ShippingAddressId;
            cart.Notes = dto.Notes;
            cart.Status = "PaymentReceived";
            cart.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(cart);

            // Agregar tracking de pago recibido
            await _orderRepository.AddTrackingAsync(new OrderTracking
            {
                OrderId = cart.OrderId,
                Status = "PaymentReceived",
                Notes = "Pago recibido - pedido confirmado"
            });

            var order = await _orderRepository.GetByIdAsync(cart.OrderId);
            return MapToOrderDto(order!);
        }

        /// <summary>
        /// Recalcula el subtotal y total del carrito.
        /// Se llama cada vez que se agrega, actualiza o elimina un producto.
        /// </summary>
        private async Task RecalcularTotalesAsync(Order cart)
        {
            var cartActualizado = await _orderRepository.GetCartByUserIdAsync(cart.UserId);
            if (cartActualizado == null) return;

            cartActualizado.Subtotal = cartActualizado.Items
                .Sum(i => i.UnitPrice * i.Quantity);
            cartActualizado.Total = cartActualizado.Subtotal + cartActualizado.ShippingCost;
            cartActualizado.UpdatedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(cartActualizado);
        }

        /// <summary>
        /// Convierte una Order de la BD a un CartDto para el frontend.
        /// </summary>
        private CartDto MapToCartDto(Order order)
        {
            return new CartDto
            {
                OrderId = order.OrderId,
                Subtotal = order.Subtotal,
                Total = order.Total,
                TotalItems = order.Items.Sum(i => i.Quantity),
                Items = order.Items.Select(i => new CartItemDto
                {
                    OrderItemId = i.OrderItemId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ProductImageUrl = i.Product?.Images
                        .FirstOrDefault(img => img.IsPrimary)?.ImageUrl,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Subtotal = i.UnitPrice * i.Quantity
                }).ToList()
            };
        }

        /// <summary>
        /// Convierte una Order de la BD a un OrderDto para el frontend.
        /// Incluye el historial de tracking con descripciones legibles.
        /// </summary>
        private OrderDto MapToOrderDto(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                DeliveryMethod = order.DeliveryMethod,
                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                Status = order.Status,
                StatusDescription = GetStatusDescription(order.Status),
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new CartItemDto
                {
                    OrderItemId = i.OrderItemId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    Subtotal = i.UnitPrice * i.Quantity
                }).ToList(),
                Tracking = order.Tracking
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => new OrderTrackingDto
                    {
                        Status = t.Status,
                        StatusDescription = GetStatusDescription(t.Status),
                        Notes = t.Notes,
                        CreatedAt = t.CreatedAt
                    }).ToList()
            };
        }

        /// <summary>
        /// Convierte el estado tecnico del pedido a un texto legible en espanol.
        /// </summary>
        private string GetStatusDescription(string status) => status switch
        {
            "PendingPayment" => "Pendiente de pago",
            "PaymentReceived" => "Pago recibido",
            "InProduction" => "En elaboracion artesanal",
            "QualityControl" => "Control de calidad",
            "Shipped" => "Enviado",
            "Delivered" => "Entregado",
            "ReadyForPickup" => "Listo para recoger",
            "PickedUp" => "Recogido",
            _ => status
        };
    }
}
