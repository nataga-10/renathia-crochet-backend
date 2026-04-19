using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Servicio para consultar el historial de pedidos del usuario.
    /// Implementa HU-09 (Ver estado del pedido).
    /// Permite al cliente ver todos sus pedidos y el tracking de cada uno.
    /// </summary>
    public class OrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// HU-09: Retorna todos los pedidos del usuario con su estado actual.
        /// Incluye el historial completo de tracking de cada pedido.
        /// </summary>
        public async Task<List<OrderDto>> GetMyOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);

            return orders
                .Where(o => o.Status != "PendingPayment") // No mostrar carritos sin pagar
                .Select(o => new OrderDto
                {
                    OrderId = o.OrderId,
                    DeliveryMethod = o.DeliveryMethod,
                    Subtotal = o.Subtotal,
                    ShippingCost = o.ShippingCost,
                    Total = o.Total,
                    Status = o.Status,
                    StatusDescription = GetStatusDescription(o.Status),
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new CartItemDto
                    {
                        OrderItemId = i.OrderItemId,
                        ProductId = i.ProductId,
                        ProductName = i.Product?.Name ?? string.Empty,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        Subtotal = i.UnitPrice * i.Quantity
                    }).ToList(),
                    Tracking = o.Tracking
                        .OrderBy(t => t.CreatedAt)
                        .Select(t => new OrderTrackingDto
                        {
                            Status = t.Status,
                            StatusDescription = GetStatusDescription(t.Status),
                            Notes = t.Notes,
                            CreatedAt = t.CreatedAt
                        }).ToList()
                }).ToList();
        }

        /// <summary>
        /// Retorna el detalle de un pedido especifico por su ID.
        /// Verifica que el pedido pertenezca al usuario que lo consulta.
        /// </summary>
        public async Task<OrderDto?> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);

            // Seguridad: solo el dueno del pedido puede verlo
            if (order == null || order.UserId != userId)
                return null;

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
        /// Convierte el estado tecnico a texto legible en espanol.
        /// Los mismos estados que usa CartService.
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