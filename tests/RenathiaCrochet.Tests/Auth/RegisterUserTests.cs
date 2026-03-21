using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Moq;
using RenathiaCrochet.Application.DTOs;
using RenathiaCrochet.Application.Services;
using RenathiaCrochet.Domain.Interfaces;

namespace RenathiaCrochet.Tests.Auth
{
    public class RegisterUserTests
    {
        [Fact]
        public async Task RegisterUser_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var mockToken = new Mock<ITokenService>();

            mockRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            var service = new AuthService(mockRepo.Object, mockToken.Object);

            // Act
            var result = await service.RegisterAsync(new RegisterDto
            {
                FullName = "Nathalia Test",
                Email = "test@correo.com",
                Password = "Password123"
            });

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Usuario registrado exitosamente");
        }

        [Fact]
        public async Task RegisterUser_WithExistingEmail_ShouldReturnError()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var mockToken = new Mock<ITokenService>();

            mockRepo.Setup(r => r.ExistsByEmailAsync("test@correo.com"))
                    .ReturnsAsync(true);

            var service = new AuthService(mockRepo.Object, mockToken.Object);

            // Act
            var result = await service.RegisterAsync(new RegisterDto
            {
                FullName = "Nathalia Test",
                Email = "test@correo.com",
                Password = "Password123"
            });

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Correo ya existente");
        }

        [Fact]
        public async Task RegisterUser_WithShortPassword_ShouldReturnError()
        {
            // Arrange
            var mockRepo = new Mock<IUserRepository>();
            var mockToken = new Mock<ITokenService>();

            mockRepo.Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
                    .ReturnsAsync(false);

            var service = new AuthService(mockRepo.Object, mockToken.Object);

            // Act
            var result = await service.RegisterAsync(new RegisterDto
            {
                FullName = "Nathalia Test",
                Email = "test@correo.com",
                Password = "123"
            });

            // Assert
            result.Success.Should().BeFalse();
            result.Message.Should().Be("La contraseña debe tener mínimo 8 caracteres");
        }
    }
}
