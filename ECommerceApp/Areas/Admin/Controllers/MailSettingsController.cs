using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MailSettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public MailSettingsController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _context.MailSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new MailSetting();
        }
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(MailSetting model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingSettings = await _context.MailSettings.FirstOrDefaultAsync();
        if (existingSettings == null)
        {
            await _context.MailSettings.AddAsync(model);
        }
        else
        {
            existingSettings.SenderName = model.SenderName;
            existingSettings.SenderEmail = model.SenderEmail;
            existingSettings.Server = model.Server;
            existingSettings.Port = model.Port;
            existingSettings.Username = model.Username;
            existingSettings.Password = model.Password;
            existingSettings.EnableSsl = model.EnableSsl;
            _context.MailSettings.Update(existingSettings);
        }

        await _context.SaveChangesAsync();
        _toast.AddSuccessToastMessage("Mail ayarları başarıyla güncellendi.", new ToastrOptions { Title = "Başarılı" });
        return RedirectToAction(nameof(Index));
    }
}
