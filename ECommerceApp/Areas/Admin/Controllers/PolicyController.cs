using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PolicyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NToastNotify.IToastNotification _toast;

        public PolicyController(ApplicationDbContext context, NToastNotify.IToastNotification toast)
        {
            _context = context;
            _toast = toast;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Policies.OrderBy(p => p.Order).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Policy policy)
        {
            // Slug form alanı olmadığı için model doğrulamasından çıkarıyoruz
            ModelState.Remove(nameof(Policy.Slug));

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(policy.Slug))
                {
                    policy.Slug = GenerateSlug(policy.Title);
                }
                _context.Add(policy);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Politika başarıyla eklendi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("Politika eklenirken bir hata oluştu.", new NToastNotify.ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Policy policy)
        {
            // Slug form alanı olmadığı için model doğrulamasından çıkarıyoruz
            ModelState.Remove(nameof(Policy.Slug));

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(policy.Slug))
                    {
                        policy.Slug = GenerateSlug(policy.Title);
                    }
                    _context.Update(policy);
                    await _context.SaveChangesAsync();
                    _toast.AddSuccessToastMessage("Politika başarıyla güncellendi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PolicyExists(policy.Id))
                    {
                        _toast.AddErrorToastMessage("Politika bulunamadı.", new NToastNotify.ToastrOptions { Title = "Hata" });
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            _toast.AddErrorToastMessage("Politika güncellenirken bir hata oluştu.", new NToastNotify.ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var policy = await _context.Policies.FindAsync(id);
            if (policy != null)
            {
                _context.Policies.Remove(policy);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Politika başarıyla silindi.", new NToastNotify.ToastrOptions { Title = "Başarılı" });
            }
            else
            {
                _toast.AddErrorToastMessage("Politika bulunamadı.", new NToastNotify.ToastrOptions { Title = "Hata" });
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PolicyExists(int id)
        {
            return _context.Policies.Any(e => e.Id == id);
        }

        private string GenerateSlug(string title)
        {
            string str = title.ToLower();
            str = str.Replace("ş", "s").Replace("ı", "i").Replace("ö", "o").Replace("ü", "u").Replace("ç", "c").Replace("ğ", "g");
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}
