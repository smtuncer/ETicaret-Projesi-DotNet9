using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductCommentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;
    private readonly IMemoryCache _cache;

    public ProductCommentController(ApplicationDbContext context, IToastNotification toast, IMemoryCache cache)
    {
        _context = context;
        _toast = toast;
        _cache = cache;
    }

    [Route("admin/product-comments")]
    public async Task<IActionResult> Index(int page = 1, bool? isApproved = null)
    {
        const int pageSize = 10;

        var query = _context.ProductComments
            .Include(c => c.Product)
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
            var comment = await _context.ProductComments.FindAsync(id);
            if (comment == null)
            {
                _toast.AddErrorToastMessage("Yorum bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            comment.IsApproved = !comment.IsApproved;
            _context.ProductComments.Update(comment);
            await _context.SaveChangesAsync();

            // Clear cache for this product
            _cache.Remove($"product-detail-{comment.ProductId}");

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string replyMessage)
    {
        try
        {
            var comment = await _context.ProductComments.FindAsync(id);
            if (comment == null)
            {
                _toast.AddErrorToastMessage("Yorum bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            comment.AdminReply = replyMessage;
            if (!string.IsNullOrWhiteSpace(replyMessage))
            {
                // If admin replies, usually we approve it too? Let's leave it manual or auto?
                // Default: Just save reply. Admin can approve separately or validation can handle.
            }

            _context.ProductComments.Update(comment);
            await _context.SaveChangesAsync();

            // Clear cache for this product
            _cache.Remove($"product-detail-{comment.ProductId}");

            _toast.AddSuccessToastMessage("Cevap kaydedildi!", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction("Index");
        }
        catch (Exception)
        {
            _toast.AddErrorToastMessage("Cevap kaydedilirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }
    }


    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var comment = await _context.ProductComments.FindAsync(id);
            if (comment == null)
            {
                _toast.AddErrorToastMessage("Yorum bulunamadı!", new ToastrOptions { Title = "Hata" });
                return RedirectToAction("Index");
            }

            var productId = comment.ProductId; // Store id before delete

            _context.ProductComments.Remove(comment);
            await _context.SaveChangesAsync();

            // Clear cache for this product
            _cache.Remove($"product-detail-{productId}");

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
