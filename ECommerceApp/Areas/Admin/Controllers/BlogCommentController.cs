using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BlogCommentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public BlogCommentController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    [Route("admin/blog-comments")]
    public async Task<IActionResult> Index(int page = 1, bool? isApproved = null)
    {
        const int pageSize = 10;

        var query = _context.BlogComments
            .Include(c => c.Blog)
            .AsQueryable();

        // Filter by approval status
        if (isApproved.HasValue)
        {
            query = query.Where(c => c.IsApproved == isApproved.Value);
        }

        var totalComments = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalComments / (double)pageSize);

        var comments = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.IsApproved = isApproved;

        return View(comments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApproval(int id)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(id);
            if (comment == null)
            {
                _toast.AddErrorToastMessage("Yorum bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            comment.IsApproved = !comment.IsApproved;
            _context.BlogComments.Update(comment);
            await _context.SaveChangesAsync();

            var message = comment.IsApproved ? "Yorum onaylandı!" : "Yorum onayı kaldırıldı!";
            _toast.AddSuccessToastMessage(message, new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            _toast.AddErrorToastMessage("İşlem sırasında bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(id);
            if (comment == null)
            {
                _toast.AddErrorToastMessage("Yorum bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            _context.BlogComments.Remove(comment);
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage("Yorum silindi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            _toast.AddErrorToastMessage("Yorum silinirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }
}
