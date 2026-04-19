using System;
using System.Collections.Generic;
using System.Text;

namespace RenathiaCrochet.Domain.Entities
{

    /// <summary>
    /// Entidad que representa un usuario registrado en el sistema.
    /// La contraseña se almacena como hash BCrypt, nunca en texto plano.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        /// <summary>RoleId 1 = Administrador, RoleId 2 = Cliente</summary>
        public int RoleId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        /// <summary>Hash generado con BCrypt. No almacenar contraseña en texto plano.</summary>
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        /// <summary>Permite bloquear usuarios sin eliminarlos de la base de datos.</summary>
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
