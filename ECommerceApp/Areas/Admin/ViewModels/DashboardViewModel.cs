using ECommerceApp.Models;

namespace ECommerceApp.Areas.Admin.ViewModels;

public class DashboardViewModel
{
    // Toplam İstatistikler
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public decimal TotalRevenue { get; set; }

    // Aylık İstatistikler
    public int MonthlyOrders { get; set; }
    public decimal MonthlyRevenue { get; set; }

    // Bekleyen İşlemler
    public int PendingOrders { get; set; }
    public int UnreadMessages { get; set; }

    // Listeler
    public List<Order> RecentOrders { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();
    public List<Product> PopularProducts { get; set; } = new();
}
