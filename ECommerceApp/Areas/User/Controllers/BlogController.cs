using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
public class BlogController : Controller
{
    private readonly ApplicationDbContext _context;

    public BlogController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Route("blog")]
    [Route("blog/sayfa-{page:int}")]
    public async Task<IActionResult> Index(string search, int? categoryId, int page = 1)
    {
        const int pageSize = 9;

        var query = _context.Blogs
            .Include(b => b.Category)
            .Include(b => b.Author)
            .Where(b => b.IsPublished)
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(search) ||
                b.Summary.ToLower().Contains(search) ||
                b.Content.ToLower().Contains(search));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        var totalBlogs = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalBlogs / (double)pageSize);

        var blogs = await query
            .OrderByDescending(b => b.PublishedDate ?? b.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _context.BlogCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .AsNoTracking()
            .ToListAsync();

        // Get recent blogs
        ViewBag.RecentBlogs = await _context.Blogs
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishedDate ?? b.CreatedDate)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        // Get recent comments
        ViewBag.RecentComments = await _context.BlogComments
            .Include(c => c.Blog)
            .Where(c => c.IsApproved)
            .OrderByDescending(c => c.CreatedDate)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return View(blogs);
    }

    [Route("blog/{slug}-{id:int}")]
    public async Task<IActionResult> Detail(string slug, int id)
    {
        var blog = await _context.Blogs
            .Include(b => b.Category)
            .Include(b => b.Author)
            .Include(b => b.Comments.Where(c => c.IsApproved))
            .FirstOrDefaultAsync(b => b.Id == id && b.IsPublished);

        if (blog == null)
        {
            return NotFound();
        }

        // Increment view count
        blog.ViewCount++;
        _context.Blogs.Update(blog);
        await _context.SaveChangesAsync();

        // Get recent blogs
        ViewBag.RecentBlogs = await _context.Blogs
            .Where(b => b.IsPublished && b.Id != blog.Id)
            .OrderByDescending(b => b.PublishedDate ?? b.CreatedDate)
            .Take(5)
            .AsNoTracking()
            .ToListAsync();

        return View(blog);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int blogId, string name, string email, string phoneNumber, string comment)
    {
        try
        {
            var blog = await _context.Blogs.FindAsync(blogId);
            if (blog == null || !blog.IsPublished)
            {
                return NotFound();
            }

            var blogComment = new Models.BlogComment
            {
                BlogId = blogId,
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber,
                Comment = comment,
                IsApproved = false, // Requires admin approval
                CreatedDate = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _context.BlogComments.AddAsync(blogComment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Yorumunuz başarıyla gönderildi. Onaylandıktan sonra yayınlanacaktır.";
            return RedirectToAction("Detail", new { slug = blog.Slug, id = blog.Id });
        }
        catch (Exception)
        {
            TempData["CommentError"] = "Yorum gönderilirken bir hata oluştu.";
            return RedirectToAction("Detail", new { slug = "", id = blogId });
        }
    }
}