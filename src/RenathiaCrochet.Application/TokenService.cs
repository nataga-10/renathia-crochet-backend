using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Implementación del servicio de generación de tokens JWT con algoritmo HS256.
    /// Lee las claves JWT_SECRET, JWT_ISSUER y JWT_AUDIENCE desde la configuración.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Genera un JWT firmado con HMAC-SHA256 que incluye los claims del usuario.
        /// El token expira 60 minutos después de su emisión.
        /// Claims incluidos: UserId, Email, FullName, RoleId.
        /// </summary>
        public string GenerateToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JWT_SECRET"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT_ISSUER"],
                audience: _configuration["JWT_AUDIENCE"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(60),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}