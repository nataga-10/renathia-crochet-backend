namespace RenathiaCrochet.Application.DTOs
{
    public class GalleryDto
    {
        public int GalleryId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
