using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{
    /// <summary>
    /// Representa una variante de color disponible para un producto.
    /// IsAvailable permite ocultar colores sin eliminarlos.
    /// </summary>
    public class ProductColor
    {
        public int ProductColorId { get; set; }
        public int ProductId { get; set; }
        public string ColorName { get; set; } = string.Empty;
        /// <summary>Código hexadecimal del color (ej: "#FF5733"). Opcional.</summary>
        public string? ColorHex { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
