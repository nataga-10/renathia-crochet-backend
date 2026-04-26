using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Application.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int Stock { get; set; }
        public bool IsMadeToOrder { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        /// <summary>Partes personalizables del producto con sus colores disponibles por parte.</summary>
        public List<ProductPartDto> Parts { get; set; } = new();
    }
}
