using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Application.DTOs
{
    /// <summary>
    /// DTO que representa un pedido completo del usuario.
    /// Se usa en HU-09 (Ver estado del pedido).
    /// Incluye los items, el estado actual y el historial de tracking.
    /// </summary>
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string DeliveryMethod { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public List<OrderTrackingDto> Tracking { get; set; } = new();
    }

    /// <summary>
    /// DTO que representa cada entrada del historial de estados.
    /// Muestra cuando cambio el estado y que nota dejo el administrador.
    /// </summary>
    public class OrderTrackingDto
    {
        public string Status { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO para finalizar la compra (HU-08).
    /// El usuario elige como quiere recibir su pedido:
    /// Shipping = envio a domicilio, Pickup = recoge en tienda.
    /// </summary>
    public class CheckoutDto
    {
        public string DeliveryMethod { get; set; } = "Shipping";
        public int? ShippingAddressId { get; set; }
        public string? Notes { get; set; }
    }
}