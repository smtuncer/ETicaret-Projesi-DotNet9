using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace ECommerceApp.Services;

public class EmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailService> _logger;

    public EmailService(ApplicationDbContext context, ILogger<EmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        await SendEmailAsync(new List<string> { toEmail }, subject, message);
    }

    public async Task SendEmailAsync(List<string> toEmails, string subject, string message)
    {
        var settings = await _context.MailSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogError("Mail settings not found in database.");
            throw new Exception("Mail ayarları bulunamadı.");
        }

        try
        {
            using (var client = new SmtpClient(settings.Server, settings.Port))
            {
                client.Credentials = new NetworkCredential(settings.Username, settings.Password);
                client.EnableSsl = settings.EnableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.SenderEmail, settings.SenderName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                foreach (var email in toEmails)
                {
                    mailMessage.To.Add(email);
                }

                await client.SendMailAsync(mailMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email.");
            throw;
        }
    }
    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Luxda.net'e Hoşgeldiniz!";
        var body = EmailTemplates.GetWelcomeEmailBody(userName);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendOrderConfirmationEmailAsync(string toEmail, Order order)
    {
        var subject = $"Siparişiniz Alındı - #{order.OrderNumber}";
        var body = EmailTemplates.GetOrderConfirmationEmailBody(order);
        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
    {
        var subject = "Şifre Sıfırlama Talebi";
        var body = EmailTemplates.GetPasswordResetEmailBody(resetLink);
        await SendEmailAsync(toEmail, subject, body);
    }
}
