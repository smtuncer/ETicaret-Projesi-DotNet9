using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FaqController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NToastNotify.IToastNotification _toast;

        public FaqController(ApplicationDbContext context, NToastNotify.IToastNotification toast)
        {
            _context = context;
            _toast = toast;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Faqs.OrderBy(f => f.Category).ThenBy(f => f.Order).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Faq faq)
        {
            if (ModelState.IsValid)
            {
                _context.Add(faq);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("SSS başarıyla eklendi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("SSS eklenirken bir hata oluştu.", new NToastNotify.ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Faq faq)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faq);
                    await _context.SaveChangesAsync();
                    _toast.AddSuccessToastMessage("SSS başarıyla güncellendi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FaqExists(faq.Id))
                    {
                        _toast.AddErrorToastMessage("SSS bulunamadı.", new NToastNotify.ToastrOptions { Title = "Hata" });
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("SSS güncellenirken bir hata oluştu.", new NToastNotify.ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq != null)
            {
                _context.Faqs.Remove(faq);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("SSS başarıyla silindi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
            }
            else
            {
                _toast.AddErrorToastMessage("SSS bulunamadı.", new NToastNotify.ToastrOptions { Title = "Hata" });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool FaqExists(int id)
        {
            return _context.Faqs.Any(e => e.Id == id);
        }
    }
}
