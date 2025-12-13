using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BrandController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public BrandController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var query = _context.Brands.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(b => b.Name.Contains(search));
        }

        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var brands = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Get Product Counts for visible brands only
        var brandIds = brands.Select(b => b.Id).ToList();
        var counts = new Dictionary<int, int>();
        if (brandIds.Any())
        {
            var rawCounts = await _context.Products
                .Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value))
                .GroupBy(p => p.BrandId)
                .Select(g => new { BrandId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var rc in rawCounts)
            {
                if (rc.BrandId.HasValue) counts[rc.BrandId.Value] = rc.Count;
            }
        }

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.ProductCounts = counts;

        return View(brands);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand brand)
    {
        if (ModelState.IsValid)
        {
            _context.Add(brand);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Marka başarıyla eklendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction(nameof(Index));
        }
        _toast.AddErrorToastMessage("Marka eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Brand brand)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(brand);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Marka güncellendi.", new ToastrOptions { Title = "Başarılı" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(brand.Id))
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
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand != null)
        {
            var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                _toast.AddErrorToastMessage("Bu markaya ait ürünler var. Önce onları silmelisiniz.", new ToastrOptions { Title = "Hata" });
            }
            else
            {
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Marka silindi.", new ToastrOptions { Title = "Başarılı" });
            }
        }
        return RedirectToAction(nameof(Index));
    }

    private bool BrandExists(int id)
    {
        return _context.Brands.Any(e => e.Id == id);
    }
}
