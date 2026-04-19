using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    /// <summary>
    /// Entidad principal del catálogo. Representa un producto de crochet con sus variantes de color e imágenes.
    /// Usa eliminación lógica: IsActive = false en lugar de borrado físico.
    /// </summary>
    public class Product
    {
        public int ProductId { get; set; }
        /// <summary>Nullable para permitir productos sin categoría asignada.</summary>
        public int? CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int Stock { get; set; } = 0;
        /// <summary>Indica si el producto se elabora bajo pedido (sin stock previo).</summary>
        public bool IsMadeToOrder { get; set; } = false;
        /// <summary>Soft delete: false oculta el producto del catálogo sin eliminarlo.</summary>
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Propiedades de navegación (cargadas con Include en EF Core)
        public Category? Category { get; set; }
        public List<ProductImage> Images { get; set; } = new();
        public List<ProductColor> Colors { get; set; } = new();
    }
}
