using System;
using System.Collections.Generic;
using System.Text;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Verificar si el correo ya existe
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Correo ya existente"
                };
            }

            // Verificar contraseña mínimo 8 caracteres
            if (dto.Password.Length < 8)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "La contraseña debe tener mínimo 8 caracteres"
                };
            }

            // Cifrar contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Crear usuario
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                RoleId = 2 // Client
            };

            await _userRepository.AddAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente"
            };
        }
    }
}