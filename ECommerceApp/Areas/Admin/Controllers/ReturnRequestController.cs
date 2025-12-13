using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReturnRequestController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReturnRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var requests = await _context.ReturnRequests
            .Include(r => r.User)
            .Include(r => r.Order)
            .Include(r => r.OrderItem)
            .ThenInclude(oi => oi.Product)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var request = await _context.ReturnRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = ReturnRequestStatus.Approved;

        // Opsiyonel: Siparişin ve ürünün durumunu da güncelleyebiliriz veya sadece talebi onaylarız.
        // İade süreci manuel devam edeceği için (para iadesi vs) sadece durumu güncelliyoruz.

        _context.Update(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "İade talebi onaylandı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var request = await _context.ReturnRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status = ReturnRequestStatus.Rejected;

        _context.Update(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "İade talebi reddedildi.";
        return RedirectToAction(nameof(Index));
    }
}
