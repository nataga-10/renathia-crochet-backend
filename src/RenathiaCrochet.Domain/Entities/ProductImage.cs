using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    /// <summary>
    /// Almacena la URL de una imagen de producto subida a Azure Blob Storage.
    /// IsPrimary indica cuál imagen se muestra en el catálogo como principal.
    /// </summary>
    public class ProductImage
    {
        public int ProductImageId { get; set; }
        public int ProductId { get; set; }
        /// <summary>URL pública del blob en Azure Storage.</summary>
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
