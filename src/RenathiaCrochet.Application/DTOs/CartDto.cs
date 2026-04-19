using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Application.DTOs
{
    /// <summary>
    /// DTO que representa el carrito completo del usuario.
    /// Se retorna cuando el usuario consulta su carrito.
    /// Contiene la lista de productos y los totales calculados.
    /// </summary>
    public class CartDto
    {
        public int OrderId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
    }

    /// <summary>
    /// DTO que representa cada producto dentro del carrito.
    /// Incluye los datos del producto para mostrarlos en la UI.
    /// </summary>
    public class CartItemDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }

    /// <summary>
    /// DTO para agregar un producto al carrito (HU-05).
    /// El usuario envia el ID del producto y la cantidad deseada.
    /// </summary>
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? ProductColorId { get; set; }
    }

    /// <summary>
    /// DTO para actualizar la cantidad de un producto en el carrito (HU-06).
    /// Solo se puede cambiar la cantidad, no el producto.
    /// </summary>
    public class UpdateCartItemDto
    {
        public int Quantity { get; set; }
    }
}