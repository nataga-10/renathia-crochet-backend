using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;

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

        public AuthController(AuthService authService)
        {
            _authService = authService;
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
    }
}
