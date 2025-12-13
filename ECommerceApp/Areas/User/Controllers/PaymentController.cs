using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class PaymentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPayTrService _payTrService;

    public PaymentController(ApplicationDbContext context, IPayTrService payTrService)
    {
        _context = context;
        _payTrService = payTrService;
    }

    [Route("odeme/havale-bilgileri/{orderId}")]
    public async Task<IActionResult> BankTransferInfo(int orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        var bankAccounts = await _context.BankAccounts.Where(b => b.IsActive).ToListAsync();

        ViewBag.BankAccounts = bankAccounts;
        ViewBag.Order = order;

        var model = new PaymentNotification
        {
            OrderId = order.Id,
            Amount = order.TotalAmount
        };

        return View(model);
    }

    [HttpPost]
    [Route("odeme/bildirim-yap")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitNotification(PaymentNotification model, IFormFile? receiptImage)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // OrderId model içinde gelmeli
        if (model.OrderId == null)
        {
            return NotFound();
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
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
        model.CreatedDate = DateTime.Now;
        model.Status = PaymentNotificationStatus.Pending;

        // Note property'si Description'a mapleniyor modelde, ama formdan Description gelebilir.
        // Eğer formdan Note geliyorsa model binder onu Description'a set eder (NotMapped wrapper sayesinde).

        _context.PaymentNotifications.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ödeme bildiriminiz başarıyla oluşturuldu. Admin onayından sonra işleme alınacaktır.";
        return RedirectToAction("BankTransferInfo", new { orderId = model.OrderId });
    }

    [Route("odeme/kredi-karti/{orderId}")]
    public async Task<IActionResult> PayTrPayment(int orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(userId);

        try
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Localhost might return ::1, PayTR needs valid IP usually, but for test it might be ok or we send dummy
            if (ip == "::1") ip = "127.0.0.1";

            string token = await _payTrService.GetIframeTokenAsync(order, user, order.Items.ToList(), ip);
            ViewBag.Token = token;
            return View(order);
        }
        catch (Exception ex)
        {
            // TempData["Error"] = "Ödeme sistemi hatası: " + ex.Message;
            // Redirect back to checkout or show error
            // return RedirectToAction("Index", "Checkout");
            return Content("Ödeme sistemi hatası: " + ex.ToString());
        }
    }

    [HttpPost]
    [Route("odeme/callback")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> PayTrCallback(IFormCollection form)
    {
        var merchantOid = form["merchant_oid"];
        var status = form["status"];
        var totalAmount = form["total_amount"];
        var hash = form["hash"];

        if (!_payTrService.ValidateCallback(merchantOid, status, totalAmount, hash))
        {
            return Content("PAYTR notification failed: bad hash");
        }

        if (status == "success")
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == merchantOid.ToString());
            if (order != null)
            {
                if (order.Status == OrderStatus.Pending) // Only update if pending
                {
                    order.Status = OrderStatus.Approved; // Or Paid
                    // order.IsPaid = true; // If you have such field
                    await _context.SaveChangesAsync();

                    try
                    {
                        var emailService = (IEmailService)HttpContext.RequestServices.GetService(typeof(IEmailService));

                        // Send User Email
                        var user = await _context.Users.FindAsync(order.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            await emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                        }

                        // Send Admin Emails
                        var adminEmails = await _context.Users
                            .Where(u => u.Role == "Admin")
                            .Select(u => u.Email)
                            .ToListAsync();

                        if (adminEmails.Any())
                        {
                            await emailService.SendEmailAsync(adminEmails, $"Sipariş Onaylandı: {order.OrderNumber}", $"Sipariş ödemesi alındı ve onaylandı. Tutar: {order.TotalAmount:C2}. Sipariş No: {order.OrderNumber}");
                        }
                    }
                    catch { }
                }
            }
        }
        else
        {
            // Payment failed
            // You might want to log this or update order status to Failed
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == merchantOid.ToString());
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled; // Or Failed
                await _context.SaveChangesAsync();
            }
        }

        return Content("OK");
    }
    [Route("odeme/iyzico/{orderId}")]
    public async Task<IActionResult> IyzicoPayment(int orderId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(userId);

        // Iyzico requires a real callback URL, it doesn't just return something to an iframe normally, 
        // but for CheckoutFormInitialize it renders a script.
        // The callback URL is where the user is redirected AFTER payment on Iyzico side.
        // We need an absolute URL.
        var request = HttpContext.Request;
        var callbackUrl = $"{request.Scheme}://{request.Host}/odeme/iyzico-callback";

        try
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (ip == "::1") ip = "127.0.0.1";

            var checkoutFormInitialize = await ((IIyzicoService)HttpContext.RequestServices.GetService(typeof(IIyzicoService)))
                .InitializeCheckoutFormAsync(order, user, order.Items.ToList(), ip, callbackUrl);

            if (checkoutFormInitialize.Status == "success")
            {
                ViewBag.CheckoutFormScript = checkoutFormInitialize.CheckoutFormContent;
                return View(order);
            }
            else
            {
                // Error initializing form
                return Content($"Iyzico Başlatma Hatası: {checkoutFormInitialize.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            return Content("Ödeme sistemi hatası: " + ex.ToString());
        }
    }

    [HttpPost]
    [Route("odeme/iyzico-callback")]
    [AllowAnonymous] // Iyzico posts back to this URL
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> IyzicoCallback(IFormCollection form)
    {
        // Iyzico returns a 'token' in the POST body
        var token = form["token"];

        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Index", "Cart"); // Or error page
        }

        // Validate payment with token
        var authResult = await ((IIyzicoService)HttpContext.RequestServices.GetService(typeof(IIyzicoService)))
            .RetrieveCheckoutFormAuthAsync(token);

        if (authResult.Status == "success" && authResult.PaymentStatus == "SUCCESS")
        {
            var orderNumber = authResult.BasketId; // We sent OrderNumber as BasketId
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order != null)
            {
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Approved;
                    await _context.SaveChangesAsync();

                    try
                    {
                        var emailService = (IEmailService)HttpContext.RequestServices.GetService(typeof(IEmailService));

                        // Send User Email
                        var user = await _context.Users.FindAsync(order.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.Email))
                        {
                            await emailService.SendOrderConfirmationEmailAsync(user.Email, order);
                        }

                        // Send Admin Emails
                        var adminEmails = await _context.Users
                            .Where(u => u.Role == "Admin")
                            .Select(u => u.Email)
                            .ToListAsync();

                        if (adminEmails.Any())
                        {
                            await emailService.SendEmailAsync(adminEmails, $"Sipariş Onaylandı: {order.OrderNumber}", $"Sipariş ödemesi alındı ve onaylandı. Tutar: {order.TotalAmount:C2}. Sipariş No: {order.OrderNumber}");
                        }
                    }
                    catch { }

                    // Clear cart via cookie/session manually if needed, 
                    // but CheckoutController already cleared it upon submitting order.
                    // Just show success page.
                    return RedirectToAction("Success", "Checkout", new { orderId = order.Id });
                }
                else
                {
                    // Already processed
                    return RedirectToAction("Success", "Checkout", new { orderId = order.Id });
                }
            }
        }

        // Payment failed or other error
        return Content($"Ödeme Başarısız! Hata: {authResult.ErrorMessage}");
    }
}

