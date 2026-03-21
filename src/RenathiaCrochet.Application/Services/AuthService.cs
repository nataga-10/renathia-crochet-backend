using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository userRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _userRepository.ExistsByEmailAsync(dto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Correo ya existente"
                };
            }

            if (dto.Password.Length < 8)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "La contraseña debe tener mínimo 8 caracteres"
                };
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Phone = dto.Phone,
                RoleId = 2
            };

            await _userRepository.AddAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente"
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Credenciales incorrectas"
                };
            }

            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Usuario bloqueado temporalmente"
                };
            }

            var token = _tokenService.GenerateToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Inicio de sesión exitoso",
                Token = token
            };
        }

        public async Task<AuthResponseDto> RecoverPasswordAsync(RecoverPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Si el correo existe, recibirás las instrucciones"
                };
            }

            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var resetLink = $"https://renathia.com/reset-password?token={resetToken}&email={user.Email}";

            return new AuthResponseDto
            {
                Success = true,
                Message = "Si el correo existe, recibirás las instrucciones"
            };
        }
    }
}