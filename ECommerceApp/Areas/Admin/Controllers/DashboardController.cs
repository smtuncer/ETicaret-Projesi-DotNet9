using ECommerceApp.Areas.Admin.ViewModels;
using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Route("admin/dashboard")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var viewModel = new DashboardViewModel
            {
                // Toplam İstatistikler
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount),

                // Bu Ay İstatistikleri
                MonthlyOrders = await _context.Orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year)
                    .CountAsync(),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month &&
                               o.OrderDate.Year == DateTime.Now.Year &&
                               o.Status == OrderStatus.Delivered)
                    .SumAsync(o => o.TotalAmount),

                // Bekleyen İşlemler
                PendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .CountAsync(),
                UnreadMessages = await _context.ContactMessages
                    .Where(m => !m.IsRead)
                    .CountAsync(),

                // Son Siparişler
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync(),

                // Düşük Stoklu Ürünler
                LowStockProducts = await _context.Products
                    .Where(p => p.Stock < 10 && p.IsActive)
                    .OrderBy(p => p.Stock)
                    .Take(5)
                    .ToListAsync(),

                // Popüler Ürünler (Öne Çıkanlar)
                PopularProducts = await _context.Products
                    .Include(p => p.Images)
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading dashboard");
            return View(new DashboardViewModel());
        }
    }
}
