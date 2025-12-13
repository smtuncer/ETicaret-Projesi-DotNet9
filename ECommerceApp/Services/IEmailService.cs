using ECommerceApp.Models;

namespace ECommerceApp.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string message);
    Task SendEmailAsync(List<string> toEmails, string subject, string message);
    Task SendWelcomeEmailAsync(string toEmail, string userName);
    Task SendOrderConfirmationEmailAsync(string toEmail, Order order);
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
}
