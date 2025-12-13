using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SiteSettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SiteSettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new SiteSettings();
            _context.SiteSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SiteSettings model)
    {
        if (ModelState.IsValid)
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SiteSettings();
                _context.SiteSettings.Add(settings);
            }

            settings.VatRate = model.VatRate;
            settings.ShippingFee = model.ShippingFee;
            settings.FreeShippingThreshold = model.FreeShippingThreshold;
            settings.ContactEmail = model.ContactEmail;
            settings.UpdatedDate = DateTime.Now;
            _context.Update(settings);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Site ayarları güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        return View("Index", model);
    }
}
