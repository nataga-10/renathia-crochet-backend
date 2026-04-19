using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    public class OrderTracking
    {
        //Cada vez que el pedido cambia de estado(PendingPayment → PaymentReceived → InProduction → Shipped) se crea un nuevo registro aquí.Es como el "rastro" del pedido.
        public int TrackingId { get; set; }
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }

        // Navegacion
        public Order? Order { get; set; }
    }
}
