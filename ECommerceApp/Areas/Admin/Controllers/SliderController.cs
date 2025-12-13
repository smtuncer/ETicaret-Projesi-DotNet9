using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SliderController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;
    private readonly IWebHostEnvironment _env;

    public SliderController(ApplicationDbContext context, IToastNotification toast, IWebHostEnvironment env)
    {
        _context = context;
        _toast = toast;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var sliders = await _context.Sliders
            .OrderBy(s => s.Order)
            .ThenByDescending(s => s.CreatedDate)
            .ToListAsync();

        return View(sliders);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Slider slider)
    {
        if (ModelState.IsValid)
        {
            // Desktop Image Upload
            if (slider.ImageUpload != null)
            {
                var uniqueFileName = GetUniqueFileName(slider.ImageUpload.FileName);
                var uploads = Path.Combine(_env.WebRootPath, "img", "slider");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }
                var filePath = Path.Combine(uploads, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await slider.ImageUpload.CopyToAsync(fileStream);
                }
                slider.ImageUrl = "/img/slider/" + uniqueFileName;
            }

            // Mobile Image Upload
            if (slider.MobileImageUpload != null)
            {
                var uniqueFileName = GetUniqueFileName(slider.MobileImageUpload.FileName);
                var uploads = Path.Combine(_env.WebRootPath, "img", "slider");
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }
                var filePath = Path.Combine(uploads, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await slider.MobileImageUpload.CopyToAsync(fileStream);
                }
                slider.MobileImageUrl = "/img/slider/" + uniqueFileName;
            }

            if (string.IsNullOrEmpty(slider.ImageUrl))
            {
                ModelState.AddModelError("ImageUpload", "Lütfen bir masaüstü görseli yükleyin.");
                _toast.AddErrorToastMessage("Görsel yüklenmedi.", new ToastrOptions { Title = "Hata" });
                return View(slider);
            }

            slider.CreatedDate = DateTime.Now;
            _context.Add(slider);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Slider başarıyla eklendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction(nameof(Index));
        }
        _toast.AddErrorToastMessage("Slider eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
        return View(slider);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var slider = await _context.Sliders.FindAsync(id);
        if (slider == null)
        {
            return NotFound();
        }
        return View(slider);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Slider slider)
    {
        if (id != slider.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingSlider = await _context.Sliders.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

                // Desktop Image Upload
                if (slider.ImageUpload != null)
                {
                    // Delete old image
                    if (!string.IsNullOrEmpty(existingSlider?.ImageUrl))
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, existingSlider.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    var uniqueFileName = GetUniqueFileName(slider.ImageUpload.FileName);
                    var uploads = Path.Combine(_env.WebRootPath, "img", "slider");
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }
                    var filePath = Path.Combine(uploads, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await slider.ImageUpload.CopyToAsync(fileStream);
                    }
                    slider.ImageUrl = "/img/slider/" + uniqueFileName;
                }
                else
                {
                    slider.ImageUrl = existingSlider?.ImageUrl;
                }

                // Mobile Image Upload
                if (slider.MobileImageUpload != null)
                {
                    // Delete old image
                    if (!string.IsNullOrEmpty(existingSlider?.MobileImageUrl))
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, existingSlider.MobileImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    var uniqueFileName = GetUniqueFileName(slider.MobileImageUpload.FileName);
                    var uploads = Path.Combine(_env.WebRootPath, "img", "slider");
                    if (!Directory.Exists(uploads))
                    {
                        Directory.CreateDirectory(uploads);
                    }
                    var filePath = Path.Combine(uploads, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await slider.MobileImageUpload.CopyToAsync(fileStream);
                    }
                    slider.MobileImageUrl = "/img/slider/" + uniqueFileName;
                }
                else
                {
                    slider.MobileImageUrl = existingSlider?.MobileImageUrl;
                }

                _context.Update(slider);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Slider güncellendi.", new ToastrOptions { Title = "Başarılı" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SliderExists(slider.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        _toast.AddErrorToastMessage("Güncelleme başarısız.", new ToastrOptions { Title = "Hata" });
        return View(slider);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var slider = await _context.Sliders.FindAsync(id);
        if (slider != null)
        {
            // Delete images
            if (!string.IsNullOrEmpty(slider.ImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath, slider.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            if (!string.IsNullOrEmpty(slider.MobileImageUrl))
            {
                var path = Path.Combine(_env.WebRootPath, slider.MobileImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.Sliders.Remove(slider);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Slider silindi.", new ToastrOptions { Title = "Başarılı" });
        }
        return RedirectToAction(nameof(Index));
    }

    private bool SliderExists(int id)
    {
        return _context.Sliders.Any(e => e.Id == id);
    }

    private string GetUniqueFileName(string fileName)
    {
        fileName = Path.GetFileName(fileName);
        return Path.GetFileNameWithoutExtension(fileName)
               + "_"
               + Guid.NewGuid().ToString().Substring(0, 4)
               + Path.GetExtension(fileName);
    }
}
