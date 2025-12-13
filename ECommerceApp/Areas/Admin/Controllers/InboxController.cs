using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class InboxController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IToastNotification _toast;

    public InboxController(ApplicationDbContext context, IEmailService emailService, IToastNotification toast)
    {
        _context = context;
        _emailService = emailService;
        _toast = toast;
    }

    public async Task<IActionResult> Index(string search, string status, int page = 1)
    {
        const int pageSize = 10;
        var query = _context.ContactMessages.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.Trim();
            query = query.Where(m => m.Subject.Contains(search) || m.Name.Contains(search) || m.Email.Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            switch (status)
            {
                case "unread":
                    query = query.Where(m => !m.IsRead);
                    break;
                case "read":
                    query = query.Where(m => m.IsRead);
                    break;
                case "replied":
                    query = query.Where(m => m.IsReplied);
                    break;
            }
        }

        var totalMessages = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalMessages / (double)pageSize);

        var messages = await query
            .OrderByDescending(m => m.SentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(messages);
    }

    public async Task<IActionResult> Details(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            _context.Update(message);
            await _context.SaveChangesAsync();
        }

        return View(message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string replyMessage)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message == null)
        {
            return NotFound();
        }

        try
        {
            var emailBody = EmailTemplates.GetGeneralEmailBody($"Cevap: {message.Subject}", replyMessage);
            await _emailService.SendEmailAsync(message.Email, $"RE: {message.Subject}", emailBody);

            message.IsReplied = true;
            _context.Update(message);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Cevap başarıyla gönderildi.", new ToastrOptions { Title = "Başarılı" });
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage($"Hata: {ex.Message}", new ToastrOptions { Title = "Hata" });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var message = await _context.ContactMessages.FindAsync(id);
        if (message != null)
        {
            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Mesaj silindi.", new ToastrOptions { Title = "Başarılı" });
        }
        return RedirectToAction(nameof(Index));
    }
}
