using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de generación de tokens JWT.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Genera un token JWT firmado con los datos del usuario (UserId, Email, FullName, RoleId).
        /// El token tiene una expiración de 60 minutos.
        /// </summary>
        string GenerateToken(User user);
    }
}