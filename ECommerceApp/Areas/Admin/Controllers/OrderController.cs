using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string search,
        string status,
        DateTime? startDate,
        DateTime? endDate,
        string sortOrder = "date_desc",
        int page = 1,
        int pageSize = 10)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .AsQueryable();

        // Filter by Status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out OrderStatus orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        // Filter by Search Query (Order Number, User Name, Email)
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                (o.User != null && (o.User.Name.Contains(search) || o.User.Surname.Contains(search) || o.User.Email.Contains(search)))
            );
        }

        // Filter by Date Range
        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // End of the day
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= end);
        }

        // Sorting
        switch (sortOrder)
        {
            case "date_asc":
                query = query.OrderBy(o => o.OrderDate);
                break;
            case "amount_desc":
                query = query.OrderByDescending(o => o.TotalAmount);
                break;
            case "amount_asc":
                query = query.OrderBy(o => o.TotalAmount);
                break;
            case "status":
                query = query.OrderBy(o => o.Status);
                break;
            case "order_number":
                query = query.OrderBy(o => o.OrderNumber);
                break;
            case "date_desc":
            default:
                query = query.OrderByDescending(o => o.OrderDate);
                break;
        }

        // Pagination
        if (pageSize < 1) pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentStatus = status;
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.CurrentSort = sortOrder;
        ViewBag.PageSize = pageSize;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcel(
        string search,
        string status,
        DateTime? startDate,
        DateTime? endDate,
        string sortOrder,
        List<string> selectedColumns)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .AsQueryable();

        // Apply same filters as Index (without pagination)
        if (!string.IsNullOrEmpty(status) && Enum.TryParse(status, out OrderStatus orderStatus))
        {
            query = query.Where(o => o.Status == orderStatus);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o =>
                o.OrderNumber.Contains(search) ||
                (o.User != null && (o.User.Name.Contains(search) || o.User.Surname.Contains(search) || o.User.Email.Contains(search)))
            );
        }

        if (startDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= end);
        }

        // Sorting
        switch (sortOrder)
        {
            case "date_asc":
                query = query.OrderBy(o => o.OrderDate);
                break;
            case "amount_desc":
                query = query.OrderByDescending(o => o.TotalAmount);
                break;
            case "amount_asc":
                query = query.OrderBy(o => o.TotalAmount);
                break;
            case "status":
                query = query.OrderBy(o => o.Status);
                break;
            case "order_number":
                query = query.OrderBy(o => o.OrderNumber);
                break;
            case "date_desc":
            default:
                query = query.OrderByDescending(o => o.OrderDate);
                break;
        }

        var orders = await query.ToListAsync();

        // Generate CSV
        var sb = new System.Text.StringBuilder();

        // Header
        var header = new List<string>();
        foreach (var col in selectedColumns)
        {
            switch (col)
            {
                case "OrderNumber": header.Add("Sipariş No"); break;
                case "CustomerName": header.Add("Müşteri Adı"); break;
                case "CustomerEmail": header.Add("Müşteri Email"); break;
                case "Status": header.Add("Durum"); break;
                case "PaymentMethod": header.Add("Ödeme Yöntemi"); break;
                case "TotalAmount": header.Add("Tutar"); break;
                case "OrderDate": header.Add("Sipariş Tarihi"); break;
                case "ShippingNote": header.Add("Kargo Notu"); break;
            }
        }
        sb.AppendLine(string.Join(";", header));

        // Rows
        foreach (var order in orders)
        {
            var row = new List<string>();
            foreach (var col in selectedColumns)
            {
                switch (col)
                {
                    case "OrderNumber":
                        row.Add($"\"{order.OrderNumber}\"");
                        break;
                    case "CustomerName":
                        row.Add($"\"{order.User?.Name} {order.User?.Surname}\"");
                        break;
                    case "CustomerEmail":
                        row.Add($"\"{order.User?.Email}\"");
                        break;
                    case "Status":
                        row.Add($"\"{order.Status}\"");
                        break;
                    case "PaymentMethod":
                        row.Add($"\"{order.PaymentMethod}\"");
                        break;
                    case "TotalAmount":
                        row.Add($"\"{order.TotalAmount:N2}\"");
                        break;
                    case "OrderDate":
                        row.Add($"\"{order.OrderDate:dd.MM.yyyy HH:mm}\"");
                        break;
                    case "ShippingNote":
                        row.Add($"\"{(order.ShippingNote ?? "").Replace("\"", "\"\"")}\"");
                        break;
                }
            }
            sb.AppendLine(string.Join(";", row));
        }

        // Return CSV file (using semicolon separator which is common for Excel in some regions, or comma. 
        // Usually comma is standard but for TR usually semicolon works better with Excel. 
        // I will use semicolon ';' as it handles decimal commas better in TR locale if opened directly)
        // Adding BOM for UTF-8 correct display in Excel
        var encoding = System.Text.Encoding.UTF8;
        var preamble = encoding.GetPreamble();
        var bytes = preamble.Concat(encoding.GetBytes(sb.ToString())).ToArray();

        return File(bytes, "text/csv", $"siparisler_{DateTime.Now:yyyyMMddHHmm}.csv");
    }

    public async Task<IActionResult> Detail(int id)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Images)
            .Include(o => o.PaymentNotifications)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        _context.Update(order);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Sipariş durumu güncellendi.";
        return RedirectToAction(nameof(Detail), new { id = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingNote(int id, string shippingNote)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.ShippingNote = shippingNote;
        order.ShippingNoteDate = DateTime.Now;
        order.ShippingNoteBy = User.Identity?.Name ?? "Admin";

        _context.Update(order);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Kargo notu güncellendi.";
        return RedirectToAction(nameof(Detail), new { id = id });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNavlungoShipment(int id, [FromServices] INavlungoService navlungoService)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        try
        {
            var result = await navlungoService.CreateShipmentAsync(order);

            order.NavlungoPostNumber = result.PostNumber;
            order.NavlungoBarcodeUrl = result.BarcodeUrl;

            // Auto update status to Shipped
            order.Status = OrderStatus.Shipped;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Navlungo gönderisi oluşturuldu. Takip No: " + result.PostNumber;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Hata: " + ex.Message;
        }

        return RedirectToAction(nameof(Detail), new { id = id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetNavlungoLabel(int id, [FromServices] INavlungoService navlungoService)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        if (string.IsNullOrEmpty(order.NavlungoPostNumber))
        {
            TempData["ErrorMessage"] = "Bu sipariş için henüz Navlungo gönderisi oluşturulmamış.";
            return RedirectToAction(nameof(Detail), new { id = id });
        }

        // If we already have the URL, redirect directly
        if (!string.IsNullOrEmpty(order.NavlungoBarcodeUrl))
        {
            return Redirect(order.NavlungoBarcodeUrl);
        }

        try
        {
            var labelUrl = await navlungoService.GetShippingLabelAsync(order.NavlungoPostNumber);
            order.NavlungoBarcodeUrl = labelUrl;
            _context.Update(order);
            await _context.SaveChangesAsync();

            return Redirect(labelUrl);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Etiket alınamadı: " + ex.Message;
            return RedirectToAction(nameof(Detail), new { id = id });
        }
    }
}
