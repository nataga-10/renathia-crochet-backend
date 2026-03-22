using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Application.DTOs
{
    public class UpdateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int Stock { get; set; }
        public bool IsMadeToOrder { get; set; }
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Colors { get; set; } = new();
    }
}