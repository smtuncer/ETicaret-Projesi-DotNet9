using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BlogController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;
    private readonly IWebHostEnvironment _env;

    public BlogController(ApplicationDbContext context, IToastNotification toast, IWebHostEnvironment env)
    {
        _context = context;
        _toast = toast;
        _env = env;
    }

    [Route("admin/blogs")]
    public async Task<IActionResult> Index(string search, int? categoryId, bool? isPublished, int page = 1)
    {
        const int pageSize = 10;

        var query = _context.Blogs
            .Include(b => b.Category)
            .Include(b => b.Author)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(search) ||
                b.Summary.ToLower().Contains(search));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        // Filter by publish status
        if (isPublished.HasValue)
        {
            query = query.Where(b => b.IsPublished == isPublished.Value);
        }

        var totalBlogs = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

        var blogs = await query
            .OrderByDescending(b => b.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.IsPublished = isPublished;
        ViewBag.TotalBlogs = totalBlogs;
        ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();

        return View(blogs);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ECommerceApp.Models.Blog blog, IFormFile? FeaturedImage)
    {
        try
        {
            // Get current user ID
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                _toast.AddErrorToastMessage("Kullanıcı bilgisi bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            blog.AuthorId = int.Parse(currentUserId);
            blog.CreatedDate = DateTime.Now;
            blog.Slug = GenerateSlug(blog.Title);

            // Handle image upload
            if (FeaturedImage != null && FeaturedImage.Length > 0)
            {
                var imageResult = await ProcessImageUploadAsync(FeaturedImage);
                if (!imageResult.Success)
                {
                    _toast.AddErrorToastMessage(imageResult.ErrorMessage, new ToastrOptions { Title = "Hata" });
                    return RedirectToAction("Index");
                }
                blog.FeaturedImage = imageResult.FilePath;
            }

            if (blog.IsPublished)
            {
                blog.PublishedDate = DateTime.Now;
            }

            await _context.Blogs.AddAsync(blog);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Blog yazısı eklendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Blog eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ECommerceApp.Models.Blog blog, IFormFile? FeaturedImage)
    {
        try
        {
            var existingBlog = await _context.Blogs.FindAsync(blog.Id);
            if (existingBlog == null)
            {
                _toast.AddErrorToastMessage("Blog bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            existingBlog.Title = blog.Title;
            existingBlog.Content = blog.Content;
            existingBlog.Summary = blog.Summary;
            existingBlog.CategoryId = blog.CategoryId;
            existingBlog.MetaTitle = blog.MetaTitle;
            existingBlog.MetaDescription = blog.MetaDescription;
            existingBlog.MetaKeywords = blog.MetaKeywords;
            existingBlog.IsFeatured = blog.IsFeatured;
            existingBlog.UpdatedDate = DateTime.Now;
            existingBlog.Slug = GenerateSlug(blog.Title);

            // Handle publish status change
            if (blog.IsPublished && !existingBlog.IsPublished)
            {
                existingBlog.PublishedDate = DateTime.Now;
            }
            existingBlog.IsPublished = blog.IsPublished;

            // Handle image upload
            if (FeaturedImage != null && FeaturedImage.Length > 0)
            {
                var imageResult = await ProcessImageUploadAsync(FeaturedImage);
                if (!imageResult.Success)
                {
                    _toast.AddErrorToastMessage(imageResult.ErrorMessage, new ToastrOptions { Title = "Hata" });
                    return RedirectToAction("Index");
                }

                // Delete old image
                if (!string.IsNullOrEmpty(existingBlog.FeaturedImage))
                {
                    DeleteFile(existingBlog.FeaturedImage);
                }

                existingBlog.FeaturedImage = imageResult.FilePath;
            }

            _context.Blogs.Update(existingBlog);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Blog güncellendi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Blog güncellenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                _toast.AddErrorToastMessage("Blog bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            // Delete image
            if (!string.IsNullOrEmpty(blog.FeaturedImage))
            {
                DeleteFile(blog.FeaturedImage);
            }

            _context.Blogs.Remove(blog);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Blog silindi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Blog silinirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> TogglePublish(int id)
    {
        try
        {
            var blog = await _context.Blogs.FindAsync(id);
            if (blog == null)
            {
                _toast.AddErrorToastMessage("Blog bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            blog.IsPublished = !blog.IsPublished;
            if (blog.IsPublished)
            {
                blog.PublishedDate = DateTime.Now;
            }

            _context.Blogs.Update(blog);
            await _context.SaveChangesAsync();

            var message = blog.IsPublished ? "Blog yayınlandı!" : "Blog yayından kaldırıldı!";
            _toast.AddSuccessToastMessage(message, new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("İşlem sırasında bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    private string GenerateSlug(string title)
    {
        if (string.IsNullOrEmpty(title)) return "";

        var slug = title.ToLower();
        slug = slug.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ").Trim();
        slug = slug.Replace(" ", "-");

        return slug;
    }

    private async Task<(bool Success, string FilePath, string ErrorMessage)> ProcessImageUploadAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(ext))
        {
            return (false, null, "Resim uzantısı .jpg, .jpeg, .png, .webp veya .gif olmalıdır.");
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var uploadPath = Path.Combine(_env.WebRootPath, "images", "blog", fileName);

        // Create directory if it doesn't exist
        var directory = Path.GetDirectoryName(uploadPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            return (false, null, $"Resim yüklenirken hata oluştu: {ex.Message}");
        }

        return (true, $"/images/blog/{fileName}", null);
    }

    private void DeleteFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
