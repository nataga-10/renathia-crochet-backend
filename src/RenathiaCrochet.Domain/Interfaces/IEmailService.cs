using RenathiaCrochet.Domain.Entities;

namespace RenathiaCrochet.Domain.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordRecoveryEmailAsync(string toEmail, string resetLink);
        Task SendOrderConfirmationAsync(Order order, string clientEmail);
    }
}
