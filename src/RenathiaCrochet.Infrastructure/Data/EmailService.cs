using System.Text;
using Microsoft.Extensions.Configuration;
using RenathiaCrochet.Domain.Entities;
using RenathiaCrochet.Domain.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace RenathiaCrochet.Infrastructure.Data
{
    /// <summary>
    /// Servicio de envío de correos mediante SendGrid HTTP API.
    /// Requiere SENDGRID_API_KEY y SMTP_USER (dirección remitente) en la configuración.
    /// No usa SMTP directo, por lo que funciona en Azure Free tier sin restricciones.
    /// </summary>
    public class EmailService : IEmailService
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

            var html = $@"
<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:0;background:#fdf6fa;font-family:Georgia,serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#fdf6fa;padding:32px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(201,110,160,0.10);'>

        <tr>
          <td style='background:#C96EA0;padding:32px 40px;text-align:center;'>
            <h1 style='margin:0;color:#ffffff;font-size:26px;letter-spacing:1px;'>Renathia Crochet</h1>
            <p style='margin:8px 0 0;color:#fde8f4;font-size:14px;'>Hecho con amor, hecho a mano ✿</p>
          </td>
        </tr>

        <tr>
          <td style='padding:36px 40px 20px;'>
            <h2 style='color:#C96EA0;margin:0 0 10px;font-size:20px;'>¡Tu pedido fue confirmado!</h2>
            <p style='color:#555;margin:0 0 6px;font-size:15px;'>
              Recibimos tu pago correctamente. Nos ponemos a trabajar en tu pedido con mucho cariño.
            </p>
            <p style='color:#888;font-size:13px;margin:0;'>Número de pedido: <strong style='color:#C96EA0;'>#{order.OrderId}</strong></p>
          </td>
        </tr>

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

        <tr>
          <td style='padding:0 40px 32px;'>
            <div style='background:#fdf0f7;border-left:4px solid #C96EA0;padding:14px 16px;border-radius:6px;font-size:14px;color:#555;'>
              <strong style='color:#C96EA0;'>Método de entrega:</strong><br/>{deliveryInfo}
            </div>
          </td>
        </tr>

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
</html>";

            await SendAsync(
                to: clientEmail,
                subject: $"¡Pedido confirmado! #{order.OrderId} - Renathia Crochet",
                html: html);
        }

        /// <summary>
        /// Envía el correo de recuperación de contraseña con el enlace de restablecimiento.
        /// El enlace expira en 30 minutos (responsabilidad del flujo que lo genera).
        /// </summary>
        public async Task SendPasswordRecoveryEmailAsync(string toEmail, string resetLink)
        {
            var html = $@"
<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'></head>
<body style='margin:0;padding:0;background:#fdf6fa;font-family:Georgia,serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#fdf6fa;padding:32px 0;'>
    <tr><td align='center'>
      <table width='560' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(201,110,160,0.10);'>

        <tr>
          <td style='background:#C96EA0;padding:28px 40px;text-align:center;'>
            <h1 style='margin:0;color:#ffffff;font-size:22px;letter-spacing:1px;'>Renathia Crochet</h1>
          </td>
        </tr>

        <tr>
          <td style='padding:36px 40px 28px;'>
            <h2 style='color:#C96EA0;margin:0 0 12px;font-size:18px;'>Recuperación de contraseña</h2>
            <p style='color:#555;margin:0 0 8px;font-size:15px;'>
              Recibimos una solicitud para restablecer tu contraseña.
              Haz clic en el botón para continuar:
            </p>
          </td>
        </tr>

        <tr>
          <td style='padding:0 40px 32px;text-align:center;'>
            <a href='{resetLink}'
               style='display:inline-block;background:#C96EA0;color:#ffffff;text-decoration:none;
                      padding:14px 36px;border-radius:8px;font-size:15px;font-weight:bold;'>
              Restablecer contraseña
            </a>
          </td>
        </tr>

        <tr>
          <td style='padding:0 40px 32px;'>
            <p style='color:#888;font-size:13px;margin:0;'>
              Este enlace expira en <strong>30 minutos</strong>.<br/>
              Si no solicitaste esto, ignora este correo — tu cuenta está segura.
            </p>
          </td>
        </tr>

        <tr>
          <td style='background:#fdf6fa;padding:20px 40px;text-align:center;border-top:1px solid #f3e0ec;'>
            <p style='margin:0;font-size:12px;color:#aaa;'>
              <strong style='color:#C96EA0;'>Renathia Crochet</strong> — hecho con amor ✿
            </p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

            await SendAsync(
                to: toEmail,
                subject: "Recuperación de contraseña - Renathia Crochet",
                html: html);
        }

        private async Task SendAsync(string to, string subject, string html)
        {
            var apiKey = _configuration["SENDGRID_API_KEY"];
            var fromEmail = _configuration["SMTP_USER"]; // reutilizamos la variable del remitente

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, "Renathia Crochet");
            var toAddress = new EmailAddress(to);
            var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, plainTextContent: null, html);

            var response = await client.SendEmailAsync(msg);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"SendGrid error {(int)response.StatusCode}: {body}");
            }
        }
    }
}
