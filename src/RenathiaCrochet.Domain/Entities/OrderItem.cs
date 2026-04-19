using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    public class OrderItem
    {
        //Si el usuario agrega 2 amigurumis y 1 llavero al carrito, se crean 2 registros en OrderItems — uno por cada producto diferente.
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductColorId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }

        // Navegacion
        public Product? Product { get; set; }
        public Order? Order { get; set; }
    }
}