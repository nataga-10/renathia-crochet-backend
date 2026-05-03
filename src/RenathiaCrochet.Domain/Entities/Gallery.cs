namespace RenathiaCrochet.Domain.Entities
{
    /// <summary>
    /// Entidad de galería. Representa una foto subida por un cliente.
    /// Solo las fotos con IsApproved = true son visibles públicamente.
    /// </summary>
    public class Gallery
    {
        public int GalleryId { get; set; }
        public int UserId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsApproved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
