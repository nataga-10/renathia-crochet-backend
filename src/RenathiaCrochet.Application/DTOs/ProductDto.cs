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
        public string? CategoryName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public List<string> Colors { get; set; } = new();
    }
}
