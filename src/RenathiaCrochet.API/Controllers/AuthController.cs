using Microsoft.AspNetCore.Mvc;
using RenathiaCrochet.Application;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;

namespace RenathiaCrochet.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("recover-password")]
        public async Task<IActionResult> RecoverPassword([FromBody] RecoverPasswordDto dto)
        {
            var result = await _authService.RecoverPasswordAsync(dto);
            return Ok(result);
        }
    }
}
