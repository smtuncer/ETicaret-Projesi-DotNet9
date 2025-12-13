using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NewsletterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IToastNotification _toast;

    public NewsletterController(ApplicationDbContext context, IEmailService emailService, IToastNotification toast)
    {
        _context = context;
        _emailService = emailService;
        _toast = toast;
    }

    [Route("admin/duyurular")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [Route("admin/duyurular/gonder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string subject, string content, bool sendToAll, string testEmail)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(content))
            {
                _toast.AddErrorToastMessage("Konu ve içerik alanları zorunludur!");
                return RedirectToAction("Index");
            }

            var emailBody = EmailTemplates.GetGeneralEmailBody(subject, content);

            // 1. Test Email
            if (!string.IsNullOrWhiteSpace(testEmail))
            {
                await _emailService.SendEmailAsync(testEmail, subject, emailBody);
                _toast.AddSuccessToastMessage($"Test emaili başarıyla gönderildi: {testEmail}");

                // If only test email was requested (sendToAll is false), we are done.
                if (!sendToAll) return RedirectToAction("Index");
            }

            // 2. Send to All Users
            if (sendToAll)
            {
                var users = await _context.Users
                    .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email)
                    .ToListAsync();

                if (users.Any())
                {
                    // Sending individual emails to avoid exposing all emails in "To" field
                    // Using parallel processing for speed, but careful with SMTP limits.
                    // Ideally this should be a background job (Hangfire). 
                    // For now, we'll do simple batch sending or loop.

                    // Simple loop implementation (Caution: might timeout for thousands of users)
                    // TODO: Move to Hangfire for better performance later.

                    // Using IEmailService to handle single sends or if it supports bulk.
                    // The interface has SendEmailAsync(List<string>...) but implementation usually does loop or bcc.
                    // Let's do a loop here or use the list overload if implemented safely.
                    // Checking IEmailService definition: Task SendEmailAsync(List<string> toEmails, string subject, string message);

                    await _emailService.SendEmailAsync(users, subject, emailBody);

                    _toast.AddSuccessToastMessage($"{users.Count} kişiye başarıyla gönderildi.");
                }
                else
                {
                    _toast.AddWarningToastMessage("Gönderilecek aktif kullanıcı bulunamadı.");
                }
            }

            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage($"Hata: {ex.Message}");
            return RedirectToAction("Index");
        }
    }
}
