using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BlogCategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public BlogCategoryController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    [Route("admin/blog-categories")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _context.BlogCategories
            .OrderBy(c => c.DisplayOrder)
            .AsNoTracking()
            .AsQueryable();

        // Pagination
        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var categories = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogCategory category)
    {
        try
        {
            category.CreatedDate = DateTime.Now;
            category.Slug = GenerateSlug(category.Name);

            await _context.BlogCategories.AddAsync(category);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kategori eklendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kategori eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogCategory category)
    {
        try
        {
            var existingCategory = await _context.BlogCategories.FindAsync(category.Id);
            if (existingCategory == null)
            {
                _toast.AddErrorToastMessage("Kategori bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.DisplayOrder = category.DisplayOrder;
            existingCategory.IsActive = category.IsActive;
            existingCategory.Slug = GenerateSlug(category.Name);

            _context.BlogCategories.Update(existingCategory);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kategori güncellendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kategori güncellenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var category = await _context.BlogCategories.FindAsync(id);
            if (category == null)
            {
                _toast.AddErrorToastMessage("Kategori bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            // Check if category has blogs
            var hasBlog = await _context.Blogs.AnyAsync(b => b.CategoryId == id);
            if (hasBlog)
            {
                _toast.AddErrorToastMessage("Bu kategoriye ait blog yazıları var, önce onları silin!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            _context.BlogCategories.Remove(category);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Kategori silindi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Kategori silinirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    private string GenerateSlug(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        var slug = name.ToLower();
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ").Trim();
        slug = slug.Replace(" ", "-");

        return slug;
    }
}
