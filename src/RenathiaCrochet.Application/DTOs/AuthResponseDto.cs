using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Application.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
    }
}
