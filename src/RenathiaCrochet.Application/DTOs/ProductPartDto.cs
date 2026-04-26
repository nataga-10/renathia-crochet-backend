namespace RenathiaCrochet.Application.DTOs
{
    /// <summary>Parte personalizable de un producto (ej: "Caparazón") con sus colores disponibles.</summary>
    public class ProductPartDto
    {
        public string PartName { get; set; } = string.Empty;
        public List<ProductColorItemDto> Colors { get; set; } = new();
    }

    public class ProductColorItemDto
    {
        public int ProductColorId { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
    }

    /// <summary>DTO de entrada para reemplazar todas las partes/colores de un producto.</summary>
    public class SetProductPartsDto
    {
        public List<PartInputDto> Parts { get; set; } = new();
    }

    public class PartInputDto
    {
        public string PartName { get; set; } = string.Empty;
        public List<ColorInputDto> Colors { get; set; } = new();
    }

    public class ColorInputDto
    {
        public string ColorName { get; set; } = string.Empty;
        public string? ColorHex { get; set; }
    }
}
