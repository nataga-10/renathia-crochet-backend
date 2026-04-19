using System;
using System.Collections.Generic;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Servicio de envío de correos mediante SMTP usando MailKit.
    /// Actualmente solo implementa el envío del correo de recuperación de contraseña.
    /// Requiere SMTP_HOST, SMTP_PORT, SMTP_USER y SMTP_PASSWORD en la configuración.
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Envía un correo HTML al usuario con el enlace para restablecer su contraseña.
        /// El enlace expira en 30 minutos (responsabilidad del flujo que lo genera).
        /// Usa StartTLS para la conexión segura con el servidor SMTP.
        /// </summary>
        public async Task SendPasswordRecoveryEmailAsync(string toEmail, string resetLink)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["SMTP_USER"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Recuperación de contraseña - RENATHIA CROCHET";

            email.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Recuperación de contraseña</h2>
                    <p>Hola, recibimos una solicitud para restablecer tu contraseña.</p>
                    <p>Haz clic en el siguiente enlace para continuar:</p>
                    <a href='{resetLink}'>Restablecer contraseña</a>
                    <p>Este enlace expira en 30 minutos.</p>
                    <p>Si no solicitaste esto, ignora este correo.</p>
                    <br/>
                    <p>RENATHIA CROCHET</p>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_configuration["SMTP_HOST"],
                int.Parse(_configuration["SMTP_PORT"]!),
                SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_configuration["SMTP_USER"],
                _configuration["SMTP_PASSWORD"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
