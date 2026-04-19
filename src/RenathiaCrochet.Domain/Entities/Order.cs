using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    public class Order
    {
        //Cuando el usuario agrega productos al carrito, se crea una Order.
        //Cuando finaliza la compra, el estado cambia a otros estados del tracking.
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string DeliveryMethod { get; set; } = "Shipping";
        public int? ShippingAddressId { get; set; }
        public int? ShippingRateId { get; set; }
        public decimal ShippingCost { get; set; } = 0;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = "PendingPayment";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navegacion
        public User? User { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public List<OrderTracking> Tracking { get; set; } = new();
    }
}
