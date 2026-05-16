using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RenathiaCrochet.Application;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;
using RenathiaCrochet.Infrastructure.Data;
using System.Security.Claims;

namespace RenathiaCrochet.API.Controllers
{
    /// <summary>
    /// Controlador de autenticación. Expone los endpoints de registro, login y recuperación de contraseña.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;

        public AuthController(AuthService authService, AppDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema.
        /// Retorna 400 si el correo ya existe o la contraseña no cumple los requisitos.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Autentica un usuario con correo y contraseña.
        /// Retorna un JWT en caso exitoso, o 400 si las credenciales son incorrectas.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// Inicia el proceso de recuperación de contraseña.
        /// Siempre retorna 200 para no revelar si el correo existe en el sistema.
        /// </summary>
        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordDto dto)
        {
            var result = await _authService.RecoverPasswordAsync(dto);
            return Ok(result);
        }

        /// <summary>
        /// Restablece la contraseña usando el token recibido por correo.
        /// Retorna 400 si el token es inválido, expiró o la contraseña no cumple los requisitos.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Retorna el perfil del usuario autenticado.
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var profile = await _authService.GetProfileAsync(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        /// <summary>
        /// Actualiza los datos del perfil del usuario autenticado.
        /// </summary>
        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _authService.UpdateProfileAsync(userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Elimina permanentemente la cuenta del usuario autenticado (solo clientes, roleId = 2).
        /// Elimina en orden: OrderTracking → OrderItems → Orders → User.
        /// </summary>
        [Authorize]
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new AuthResponseDto { Success = false, Message = "Usuario no encontrado" });

            if (user.RoleId != 2)
                return StatusCode(403, new AuthResponseDto { Success = false, Message = "Solo los clientes pueden eliminar su cuenta desde esta opción" });

            // Obtener todos los pedidos del usuario con sus hijos
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderId).ToList();

            if (orderIds.Any())
            {
                // 1. Eliminar tracking
                var tracking = await _context.OrderTracking
                    .Where(t => orderIds.Contains(t.OrderId))
                    .ToListAsync();
                _context.OrderTracking.RemoveRange(tracking);

                // 2. Eliminar items
                var items = await _context.OrderItems
                    .Where(i => orderIds.Contains(i.OrderId))
                    .ToListAsync();
                _context.OrderItems.RemoveRange(items);

                // 3. Eliminar pedidos
                _context.Orders.RemoveRange(orders);
            }

            // 4. Eliminar usuario
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Ok(new AuthResponseDto { Success = true, Message = "Cuenta eliminada exitosamente" });
        }
    }
}
