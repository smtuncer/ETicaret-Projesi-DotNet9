using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SiteContentsController : Controller
{
    private readonly ISiteContentService _siteContentService;
    private readonly IToastNotification _toast;
    private readonly ILogger<SiteContentsController> _logger;

    public SiteContentsController(
        ISiteContentService siteContentService,
        IToastNotification toast,
        ILogger<SiteContentsController> logger)
    {
        _siteContentService = siteContentService;
        _toast = toast;
        _logger = logger;
    }

    [Route("admin/icerik-yonetimi/guncelle")]
    public async Task<IActionResult> Edit()
    {
        try
        {
            var siteContent = await _siteContentService.GetSiteContentAsync();
            if (siteContent == null)
            {
                _logger.LogWarning("Site content not found, redirecting to create");
                return RedirectToAction(nameof(Create));
            }

            return View(siteContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading site content for edit");
            _toast.AddErrorToastMessage("İçerik yüklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [Route("admin/icerik-yonetimi/guncelle")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SiteContent model)
    {
        try
        {
            await _siteContentService.UpdateSiteContentAsync(model);
            _toast.AddSuccessToastMessage("İçerik başarıyla güncellendi. Tüm cache'ler temizlendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Edit");
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Site content not found for update with ID: {ContentId}", model.Id);
            _toast.AddErrorToastMessage("Hata: İçerik bulunamadı.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Edit");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while updating site content");
            _toast.AddErrorToastMessage($"Hata: {ex.Message}", new ToastrOptions { Title = "Hata" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating site content");
            _toast.AddErrorToastMessage($"Beklenmeyen hata: {ex.Message}", new ToastrOptions { Title = "Hata" });
        }

        return View(model);
    }

    [Route("admin/icerik-yonetimi/ekle")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var siteContent = await _siteContentService.GetSiteContentAsync();
            if (siteContent != null)
            {
                return RedirectToAction(nameof(Edit));
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading create site content page");
            _toast.AddErrorToastMessage("Sayfa yüklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index", "Dashboard");
        }
    }

    [Route("admin/icerik-yonetimi/ekle")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteContent model)
    {
        try
        {
            await _siteContentService.CreateSiteContentAsync(model);
            _toast.AddSuccessToastMessage("İçerik başarıyla eklendi. Tüm cache'ler temizlendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Edit");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation while creating site content");
            _toast.AddErrorToastMessage($"Hata: {ex.Message}", new ToastrOptions { Title = "Hata" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while creating site content");
            _toast.AddErrorToastMessage($"Beklenmeyen hata: {ex.Message}", new ToastrOptions { Title = "Hata" });
        }

        return View(model);
    }
}
