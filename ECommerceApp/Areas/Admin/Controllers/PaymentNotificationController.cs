using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PaymentNotificationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ECommerceApp.Services.IEmailService _emailService;

    public PaymentNotificationController(ApplicationDbContext context, ECommerceApp.Services.IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _context.PaymentNotifications
            .Include(p => p.Order)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedDate)
            .AsQueryable();

        // Pagination
        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var notification = await _context.PaymentNotifications
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (notification == null)
        {
            return NotFound();
        }

        notification.Status = PaymentNotificationStatus.Approved;

        // Update Order Status
        if (notification.Order != null)
        {
            notification.Order.Status = OrderStatus.Approved;
            _context.Update(notification.Order);
        }

        _context.Update(notification);
        await _context.SaveChangesAsync();

        // Send Order Confirmation Email
        if (notification.Order != null)
        {
            try
            {
                // We need to fetch the user email. Since we included User in the query above (line 50 is just Order), let's ensure we have it or get it from notification.UserId
                // Actually line 50 only includes Order. We should include User or depend on Order.User if mapped, or fetch it.
                // The notification object has a User navigation property usually.
                // Re-fetch to be safe or adjust Include above.

                // Let's rely on the previous fetch but let's correct line 50 to include User as well to be efficient
                // But since I can't change line 50 in this specific chunk easily without context risk, I will fetch user email manually if needed.
                // Wait, notification object usually has UserId.

                var user = await _context.Users.FindAsync(notification.UserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendOrderConfirmationEmailAsync(user.Email, notification.Order);
                }
            }
            catch
            {
                // logging
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var notification = await _context.PaymentNotifications
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (notification == null)
        {
            return NotFound();
        }

        notification.Status = PaymentNotificationStatus.Rejected;

        // If payment is rejected, revert order status to Pending (waiting for payment)
        if (notification.Order != null)
        {
            notification.Order.Status = OrderStatus.Pending;
            _context.Update(notification.Order);
        }

        _context.Update(notification);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
