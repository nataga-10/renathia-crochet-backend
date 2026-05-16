using System.ComponentModel.DataAnnotations;

namespace RenathiaCrochet.Application.DTOs
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*]).{8,}$",
            ErrorMessage = "La contraseña debe tener mínimo 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial (!@#$%^&*)")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
