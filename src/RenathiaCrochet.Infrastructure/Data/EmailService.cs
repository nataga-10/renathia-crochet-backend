using System;
using System.Collections.Generic;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Servicio de envío de correos mediante SMTP usando MailKit.
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
        /// Envía el correo de confirmación de pedido al cliente cuando el pago es aprobado.
        /// Incluye número de pedido, productos, totales y método de entrega.
        /// </summary>
        public async Task SendOrderConfirmationAsync(Order order, string clientEmail)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_configuration["SMTP_USER"]));
            email.To.Add(MailboxAddress.Parse(clientEmail));
            email.Subject = $"¡Pedido confirmado! #{order.OrderId} - Renathia Crochet";

            var itemsHtml = new StringBuilder();
            foreach (var item in order.Items)
            {
                var productName = item.Product?.Name ?? $"Producto #{item.ProductId}";
                var subtotal = item.UnitPrice * item.Quantity;
                itemsHtml.Append($@"
                    <tr>
                        <td style='padding:10px 8px;border-bottom:1px solid #f3e0ec;'>{productName}</td>
                        <td style='padding:10px 8px;border-bottom:1px solid #f3e0ec;text-align:center;'>{item.Quantity}</td>
                        <td style='padding:10px 8px;border-bottom:1px solid #f3e0ec;text-align:right;'>${item.UnitPrice:N0}</td>
                        <td style='padding:10px 8px;border-bottom:1px solid #f3e0ec;text-align:right;'>${subtotal:N0}</td>
                    </tr>");
            }

            var deliveryInfo = order.DeliveryMethod == "Pickup"
                ? "Recogida en punto"
                : $"Envío a domicilio — {order.ShippingAddress}";

            email.Body = new TextPart("html")
            {
                Text = $@"
<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#fdf6fa;font-family:Georgia,serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#fdf6fa;padding:32px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(201,110,160,0.10);'>

        <!-- Cabecera -->
        <tr>
          <td style='background:#C96EA0;padding:32px 40px;text-align:center;'>
            <h1 style='margin:0;color:#ffffff;font-size:26px;letter-spacing:1px;'>Renathia Crochet</h1>
            <p style='margin:8px 0 0;color:#fde8f4;font-size:14px;'>Hecho con amor, hecho a mano ✿</p>
          </td>
        </tr>

        <!-- Mensaje principal -->
        <tr>
          <td style='padding:36px 40px 20px;'>
            <h2 style='color:#C96EA0;margin:0 0 10px;font-size:20px;'>¡Tu pedido fue confirmado!</h2>
            <p style='color:#555;margin:0 0 6px;font-size:15px;'>
              Recibimos tu pago correctamente. Nos ponemos a trabajar en tu pedido con mucho cariño.
            </p>
            <p style='color:#888;font-size:13px;margin:0;'>Número de pedido: <strong style='color:#C96EA0;'>#{order.OrderId}</strong></p>
          </td>
        </tr>

        <!-- Tabla de productos -->
        <tr>
          <td style='padding:0 40px 28px;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;font-size:14px;'>
              <thead>
                <tr style='background:#fdf0f7;'>
                  <th style='padding:10px 8px;text-align:left;color:#C96EA0;font-weight:600;'>Producto</th>
                  <th style='padding:10px 8px;text-align:center;color:#C96EA0;font-weight:600;'>Cant.</th>
                  <th style='padding:10px 8px;text-align:right;color:#C96EA0;font-weight:600;'>Precio</th>
                  <th style='padding:10px 8px;text-align:right;color:#C96EA0;font-weight:600;'>Subtotal</th>
                </tr>
              </thead>
              <tbody style='color:#444;'>
                {itemsHtml}
              </tbody>
            </table>
          </td>
        </tr>

        <!-- Totales -->
        <tr>
          <td style='padding:0 40px 28px;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='font-size:14px;color:#555;'>
              <tr>
                <td style='padding:4px 0;'>Subtotal</td>
                <td style='text-align:right;padding:4px 0;'>${order.Subtotal:N0} COP</td>
              </tr>
              <tr>
                <td style='padding:4px 0;'>Costo de envío</td>
                <td style='text-align:right;padding:4px 0;'>${order.ShippingCost:N0} COP</td>
              </tr>
              <tr>
                <td style='padding:10px 0 0;font-size:16px;font-weight:bold;color:#C96EA0;'>Total pagado</td>
                <td style='text-align:right;padding:10px 0 0;font-size:16px;font-weight:bold;color:#C96EA0;'>${order.Total:N0} COP</td>
              </tr>
            </table>
          </td>
        </tr>

        <!-- Entrega -->
        <tr>
          <td style='padding:0 40px 32px;'>
            <div style='background:#fdf0f7;border-left:4px solid #C96EA0;padding:14px 16px;border-radius:6px;font-size:14px;color:#555;'>
              <strong style='color:#C96EA0;'>Método de entrega:</strong><br/>{deliveryInfo}
            </div>
          </td>
        </tr>

        <!-- Pie -->
        <tr>
          <td style='background:#fdf6fa;padding:24px 40px;text-align:center;border-top:1px solid #f3e0ec;'>
            <p style='margin:0;font-size:13px;color:#aaa;'>
              ¿Tienes alguna pregunta? Escríbenos y con gusto te ayudamos.<br/>
              <strong style='color:#C96EA0;'>Renathia Crochet</strong> — hecho con amor ✿
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>"
            };

            await SendAsync(email);
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

            await SendAsync(email);
        }

        private async Task SendAsync(MimeMessage email)
        {
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
