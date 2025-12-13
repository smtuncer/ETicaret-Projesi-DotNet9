using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SeoSettingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toast;
        private readonly ISeoService _seoService;

        public SeoSettingController(ApplicationDbContext context, IToastNotification toast, ISeoService seoService)
        {
            _context = context;
            _toast = toast;
            _seoService = seoService;
        }

        public async Task<IActionResult> Index()
        {
            var list = await _context.SeoSettings.OrderBy(x => x.UrlPath).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeoSetting model)
        {
            if (ModelState.IsValid)
            {
                // Normalize path
                if (!model.UrlPath.StartsWith("/")) model.UrlPath = "/" + model.UrlPath;

                // Check duplicate
                if (await _context.SeoSettings.AnyAsync(x => x.UrlPath == model.UrlPath))
                {
                    _toast.AddErrorToastMessage("Bu URL yolu için zaten bir kayıt var.", new ToastrOptions { Title = "Hata" });
                    return RedirectToAction(nameof(Index));
                }

                model.UpdatedDate = DateTime.Now;
                _context.Add(model);
                await _context.SaveChangesAsync();

                _seoService.ClearCache(); // Cache temizle
                _toast.AddSuccessToastMessage("SEO ayarı eklendi.", new ToastrOptions { Title = "Başarılı" });
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("Ekleme başarısız. Lütfen alanları kontrol edin.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SeoSetting model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.SeoSettings.FindAsync(model.Id);
                if (existing == null) return NotFound();

                // Normalize path
                if (!model.UrlPath.StartsWith("/")) model.UrlPath = "/" + model.UrlPath;

                // Check duplicate (excluding self)
                if (await _context.SeoSettings.AnyAsync(x => x.UrlPath == model.UrlPath && x.Id != model.Id))
                {
                    _toast.AddErrorToastMessage("Bu URL yolu başka bir kayıtta kullanılıyor.", new ToastrOptions { Title = "Hata" });
                    return RedirectToAction(nameof(Index));
                }

                existing.PageName = model.PageName;
                existing.UrlPath = model.UrlPath;
                existing.Title = model.Title;
                existing.Description = model.Description;
                existing.Keywords = model.Keywords;
                existing.UpdatedDate = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                _seoService.ClearCache(); // Cache temizle
                _toast.AddSuccessToastMessage("SEO ayarı güncellendi.", new ToastrOptions { Title = "Başarılı" });
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("Güncelleme başarısız.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.SeoSettings.FindAsync(id);
            if (item != null)
            {
                _context.SeoSettings.Remove(item);
                await _context.SaveChangesAsync();
                _seoService.ClearCache(); // Cache temizle
                _toast.AddSuccessToastMessage("Kayıt silindi.", new ToastrOptions { Title = "Başarılı" });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

