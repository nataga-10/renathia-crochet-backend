namespace RenathiaCrochet.Application.DTOs
{
    public class UpdateUserAdminDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
