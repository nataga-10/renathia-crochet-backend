using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// CRUD de usuarios. Solo accesible por Administrador (RoleId = 1).
    /// </summary>
    [ApiController]
    [Route("api/Admin/users")]
    [Authorize(Roles = "1")]
    public class UsersAdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UsersAdminController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>Lista todos los usuarios del sistema.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            var result = users.Select(u => MapToDto(u));
            return Ok(result);
        }

        /// <summary>Obtiene un usuario por ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });
            return Ok(MapToDto(user));
        }

        /// <summary>Crea un nuevo usuario con rol específico.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserAdminDto dto)
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                return BadRequest(new AuthResponseDto { Success = false, Message = "El correo ya está registrado" });

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                RoleId = dto.RoleId,
                IsActive = true
            };

            await _userRepository.AddAsync(user);
            return Ok(new AuthResponseDto { Success = true, Message = "Usuario creado exitosamente" });
        }

        /// <summary>Edita nombre, email, teléfono, rol y estado activo de un usuario.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserAdminDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.RoleId = dto.RoleId;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return Ok(new AuthResponseDto { Success = true, Message = "Usuario actualizado exitosamente" });
        }

        /// <summary>Elimina un usuario por ID.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "Usuario no encontrado" });

            await _userRepository.DeleteAsync(user);
            return Ok(new AuthResponseDto { Success = true, Message = "Usuario eliminado exitosamente" });
        }

        private static UserProfileDto MapToDto(User u) => new()
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            DocumentType = u.DocumentType,
            DocumentNumber = u.DocumentNumber,
            ProfileImageUrl = u.ProfileImageUrl,
            RoleId = u.RoleId,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
