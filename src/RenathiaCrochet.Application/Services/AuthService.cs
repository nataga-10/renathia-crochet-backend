using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Application.Services
{
    /// <summary>
    /// Servicio de lógica de negocio para autenticación de usuarios.
    /// Gestiona registro, login y recuperación de contraseña.
    /// </summary>
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IEmailService emailService, ILogger<AuthService> logger, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Registra un nuevo usuario con rol de cliente (RoleId = 2).
        /// Valida que el correo no esté duplicado y que la contraseña tenga al menos 8 caracteres.
        /// La contraseña se hashea con BCrypt antes de persistirse.
        /// </summary>
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

            // Hashear la contraseña con BCrypt antes de guardarla
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Phone = dto.Phone,
                RoleId = 2 // Rol cliente por defecto
            };

            await _userRepository.AddAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Usuario registrado exitosamente"
            };
        }

        /// <summary>
        /// Autentica al usuario verificando credenciales con BCrypt.
        /// Verifica también que la cuenta esté activa antes de generar el token JWT.
        /// </summary>
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            // Verificar existencia del usuario y contraseña en un solo paso (evita enumeración)
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

        /// <summary>
        /// Inicia el flujo de recuperación de contraseña.
        /// Genera un token URL-safe y construye el enlace de restablecimiento.
        /// NOTA: El envío del correo aún no está implementado (pendiente integrar EmailService).
        /// Siempre retorna el mismo mensaje para no revelar si el correo existe (seguridad).
        /// </summary>
        public async Task<UserProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DocumentType = user.DocumentType,
                DocumentNumber = user.DocumentNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                RoleId = user.RoleId,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<AuthResponseDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "Usuario no encontrado" };

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.DocumentType = dto.DocumentType;
            user.DocumentNumber = dto.DocumentNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return new AuthResponseDto { Success = true, Message = "Perfil actualizado exitosamente" };
        }

        /// <summary>
        /// Restablece la contraseña usando el token enviado por correo.
        /// Valida que el token exista, coincida y no haya expirado.
        /// </summary>
        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            if (user == null
                || user.PasswordResetToken != dto.Token
                || user.PasswordResetTokenExpiry == null
                || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "El enlace de restablecimiento no es válido o ha expirado"
                };
            }

            if (dto.NewPassword.Length < 8)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "La contraseña debe tener mínimo 8 caracteres"
                };
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Contraseña restablecida exitosamente"
            };
        }

        public async Task<AuthResponseDto> RecoverPasswordAsync(RecoverPasswordDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);

            // Retornar el mismo mensaje independientemente de si el correo existe o no
            if (user == null)
            {
                _logger.LogInformation("RecoverPassword - correo no encontrado en BD: {Email}", dto.Email);
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Si el correo existe, recibirás las instrucciones"
                };
            }

            _logger.LogInformation("1. Usuario encontrado: {Email}", user.Email);

            // Generar token URL-safe a partir de un GUID para el enlace de restablecimiento
            var resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            // Persistir el token y su expiración (30 min) para poder verificarlo al resetear
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);
            await _userRepository.UpdateAsync(user);

            var frontendUrl = _configuration["FRONTEND_URL"]?.TrimEnd('/') ?? "https://renathia.com";
            var resetLink = $"{frontendUrl}/reset-password?token={resetToken}&email={user.Email}";

            _logger.LogInformation("2. Antes de llamar SendGrid");
            await _emailService.SendPasswordRecoveryEmailAsync(user.Email, resetLink);
            _logger.LogInformation("3. SendGrid llamado exitosamente");

            return new AuthResponseDto
            {
                Success = true,
                Message = "Si el correo existe, recibirás las instrucciones"
            };
        }
    }
}