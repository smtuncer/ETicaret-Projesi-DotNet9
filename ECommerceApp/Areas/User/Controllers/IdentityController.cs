using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
[Authorize]
[Route("")] // Controller level route prefix empty to allow absolute routes on actions to work from root
public class IdentityController : Controller
{
    private readonly ApplicationDbContext _context;

    public IdentityController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Route("hesabim")]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    [HttpPost]
    [Route("profil-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ECommerceApp.Models.User model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        // E-posta benzersizliği kontrolü
        if (model.Email != user.Email)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != userId);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
                return View("Index", user);
            }
        }

        bool emailChanged = model.Email != user.Email;

        user.Name = model.Name;
        user.Surname = model.Surname;
        user.PhoneNumber = model.PhoneNumber;
        user.IdentityNumber = model.IdentityNumber;
        user.Email = model.Email;

        _context.Update(user);
        await _context.SaveChangesAsync();

        if (emailChanged)
        {
            // E-posta değiştiği için oturum bilgilerini (Claims) güncellememiz gerekir.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Mevcut oturum durumuna göre ayarlanabilir ama varsayılan olarak kalıcı yapalım.
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            // Mevcut oturumu sonlandırıp yeni bilgilerle oturum açıyoruz.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        TempData["SuccessMessage"] = "Profil bilgileriniz güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("sifre-degistir")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ECommerceApp.Models.ViewModels.ChangePasswordVM model)
    {
        if (!ModelState.IsValid)
        {
            // Hataları TempData ile taşıyamayız, bu yüzden Index'e geri dönüp hatayı göstermek zor olabilir.
            // En iyisi model state hatalarını bir listeye çevirip TempData'ya atmak veya ayrı bir view kullanmak.
            // Kullanıcı deneyimi için Index sayfasına geri dönüp hataları gösterelim.
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ErrorMessage"] = "Şifre değiştirme hatası: " + string.Join(", ", errors);
            return RedirectToAction(nameof(Index));
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password))
        {
            TempData["ErrorMessage"] = "Mevcut şifreniz hatalı.";
            return RedirectToAction(nameof(Index));
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [Route("adreslerim")]
    public async Task<IActionResult> Addresses()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return View(addresses);
    }

    [HttpPost]
    [Route("adres-ekle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(Address address)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        address.UserId = userId;

        if (ModelState.IsValid)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Adres başarıyla eklendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Adres eklenirken bir hata oluştu.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [Route("adres-sil")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address != null)
        {
            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Adres başarıyla silindi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Adres bulunamadı.";
        }

        return RedirectToAction(nameof(Addresses));
    }

    [Route("adres-duzenle/{id}")]
    public async Task<IActionResult> EditAddress(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (address == null)
        {
            TempData["ErrorMessage"] = "Adres bulunamadı.";
            return RedirectToAction(nameof(Addresses));
        }

        return View(address);
    }

    [HttpPost]
    [Route("adres-guncelle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAddress(Address address)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var existingAddress = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == address.Id && a.UserId == userId);

        if (existingAddress == null)
        {
            TempData["ErrorMessage"] = "Adres bulunamadı.";
            return RedirectToAction(nameof(Addresses));
        }

        // Remove User from ModelState validation
        ModelState.Remove("User");
        ModelState.Remove("UserId");

        if (ModelState.IsValid)
        {
            existingAddress.Title = address.Title;
            existingAddress.FullName = address.FullName;
            existingAddress.Phone = address.Phone;
            existingAddress.Country = address.Country;
            existingAddress.City = address.City;
            existingAddress.District = address.District;
            existingAddress.OpenAddress = address.OpenAddress;
            existingAddress.ZipCode = address.ZipCode;
            existingAddress.IsBillingAddress = address.IsBillingAddress;
            existingAddress.IdentityNumber = address.IdentityNumber;
            existingAddress.CompanyName = address.CompanyName;
            existingAddress.TaxOffice = address.TaxOffice;
            existingAddress.TaxNumber = address.TaxNumber;
            existingAddress.BillingAddress = address.BillingAddress;

            _context.Update(existingAddress);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Adres başarıyla güncellendi.";
        }
        else
        {
            // Log validation errors for debugging
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            TempData["ErrorMessage"] = "Adres güncellenirken bir hata oluştu: " + string.Join(", ", errors);
            return View("EditAddress", address);
        }

        return RedirectToAction(nameof(Addresses));
    }

    [Route("siparislerim")]
    public async Task<IActionResult> Orders(int page = 1)
    {
        var pageSize = 10;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var query = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            // Show only if Approved/Shipped/Delivered/Refunded OR (Pending AND BankTransfer)
            .Where(o => o.Status != OrderStatus.Pending || o.PaymentMethod == PaymentMethod.BankTransfer)
            .OrderByDescending(o => o.OrderDate);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var orders = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(orders);
    }

    [Route("siparis-detay/{id}")]
    public async Task<IActionResult> OrderDetail(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Images)
            .Include(o => o.PaymentNotifications)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [Route("odeme-bildirimlerim")]
    public async Task<IActionResult> PaymentNotifications(int page = 1)
    {
        var pageSize = 10;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var query = _context.PaymentNotifications
            .Include(p => p.Order)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.NotificationDate);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(notifications);
    }

    [Route("degerlendirmelerim")]
    public async Task<IActionResult> Reviews(int page = 1)
    {
        var pageSize = 10;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var query = _context.ProductComments
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedDate);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(reviews);
    }


    [Route("favorilerim")]
    public async Task<IActionResult> Favorites(int page = 1)
    {
        var pageSize = 12; // 3 or 4 per row
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var query = _context.Favorites
            .Include(f => f.Product)
            .ThenInclude(p => p.Images)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedDate);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var favorites = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(favorites);
    }

    [Route("yeni-odeme-bildirimi")]
    public async Task<IActionResult> CreatePaymentNotification(int? orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Banka hesaplarını getir
        var bankAccounts = await _context.BankAccounts
            .Where(b => b.IsActive)
            .OrderBy(b => b.BankName)
            .ToListAsync();

        // Kullanıcının bekleyen ve havale/EFT ile ödenmesi gereken siparişlerini getir
        var userOrders = await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending && o.PaymentMethod == PaymentMethod.BankTransfer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        ViewBag.BankAccounts = bankAccounts;
        ViewBag.UserOrders = userOrders;

        // Eğer sipariş ID'si verilmişse ve bu sipariş kullanıcının ise, modeli hazırla
        var model = new PaymentNotification();
        if (orderId.HasValue)
        {
            var selectedOrder = userOrders.FirstOrDefault(o => o.Id == orderId.Value);
            if (selectedOrder != null)
            {
                model.OrderId = orderId.Value;
                model.Amount = selectedOrder.TotalAmount; // Sipariş tutarını otomatik doldur
            }
        }

        return View(model);
    }

    [HttpPost]
    [Route("yeni-odeme-bildirimi")]
    public async Task<IActionResult> CreatePaymentNotification(PaymentNotification model, IFormFile? receiptImage)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Banka hesaplarını ve siparişleri tekrar yükle (hata durumunda)
        var bankAccounts = await _context.BankAccounts
            .Where(b => b.IsActive)
            .OrderBy(b => b.BankName)
            .ToListAsync();

        var userOrders = await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        if (model.OrderId == 0)
        {
            ModelState.AddModelError("OrderId", "Lütfen bir sipariş seçiniz.");
        }
        else
        {
            var selectedOrder = userOrders.FirstOrDefault(o => o.Id == model.OrderId);
            if (selectedOrder == null)
            {
                ModelState.AddModelError("OrderId", "Geçersiz sipariş seçimi.");
            }
        }

        ViewBag.BankAccounts = bankAccounts;
        ViewBag.UserOrders = userOrders;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Dekont yükleme
        if (receiptImage != null && receiptImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{receiptImage.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await receiptImage.CopyToAsync(fileStream);
            }

            model.ReceiptImageUrl = $"/uploads/receipts/{uniqueFileName}";
        }

        model.UserId = userId;
        model.NotificationDate = DateTime.Now;
        model.Status = PaymentNotificationStatus.Pending;

        _context.PaymentNotifications.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ödeme bildiriminiz başarıyla oluşturuldu. Admin onayından sonra işleme alınacaktır.";
        return RedirectToAction(nameof(PaymentNotifications));
    }

    [Route("iade-talebi-formu")]
    public async Task<IActionResult> CreateReturnRequest(int orderId, int orderItemId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId && oi.OrderId == orderId && oi.Order.UserId == userId);

        if (orderItem == null)
        {
            return NotFound();
        }

        // Daha önce talep var mı kontrol et
        var existingRequest = await _context.ReturnRequests
            .AnyAsync(r => r.OrderItemId == orderItemId && r.Status != ReturnRequestStatus.Cancelled && r.Status != ReturnRequestStatus.Rejected);

        if (existingRequest)
        {
            TempData["ErrorMessage"] = "Bu ürün için zaten aktif bir iade talebiniz bulunmaktadır.";
            return RedirectToAction(nameof(OrderDetail), new { id = orderId });
        }

        var model = new ReturnRequest
        {
            OrderId = orderId,
            OrderItemId = orderItemId,
            UserId = userId,
            OrderItem = orderItem // View'da ürün bilgisini göstermek için
        };

        return View(model);
    }

    [HttpPost]
    [Route("iade-talebi-formu")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReturnRequest(ReturnRequest model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Güvenlik kontrolü: Kullanıcı gerçekten bu ürünü almış mı?
        var orderItem = await _context.OrderItems
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == model.OrderItemId && oi.OrderId == model.OrderId && oi.Order.UserId == userId);

        if (orderItem == null)
        {
            return NotFound();
        }



        ModelState.Remove("Order");
        ModelState.Remove("OrderItem");
        ModelState.Remove("User");
        ModelState.Remove("UserId");

        if (!ModelState.IsValid)
        {
            // Validasyon hatası varsa OrderItem bilgisini tekrar yükleyip view'a gönder
            model.OrderItem = orderItem;
            // Product bilgisini de yükleyelim view'da lazımsa
            await _context.Entry(orderItem).Reference(oi => oi.Product).LoadAsync();
            if (orderItem.Product != null)
            {
                await _context.Entry(orderItem.Product).Collection(p => p.Images).LoadAsync();
            }
            return View(model);
        }

        model.UserId = userId;
        model.Status = ReturnRequestStatus.Pending;
        model.CreatedDate = DateTime.Now;

        _context.ReturnRequests.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "İade talebiniz başarıyla oluşturuldu.";
        return RedirectToAction(nameof(ReturnRequests));
    }

    [Route("iade-taleplerim")]
    public async Task<IActionResult> ReturnRequests(int page = 1)
    {
        var pageSize = 10;
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var query = _context.ReturnRequests
            .Include(r => r.Order)
            .Include(r => r.OrderItem)
            .ThenInclude(oi => oi.Product)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedDate);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var requests = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;

        return View(requests);
    }

    [Route("cikis")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
