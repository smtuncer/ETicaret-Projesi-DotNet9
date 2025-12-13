using ECommerceApp.Areas.User.ViewModels;
using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly Services.ICartService _cartService;
    private readonly Services.IEmailService _emailService;

    public CheckoutController(ApplicationDbContext context, Services.ICartService cartService, Services.IEmailService emailService)
    {
        _context = context;
        _cartService = cartService;
        _emailService = emailService;
    }

    [Route("odeme")]
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var cart = await _cartService.GetCartAsync(HttpContext);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        var addresses = await _context.Addresses
            .Where(a => a.UserId == userId)
            .ToListAsync();

        var viewModel = new CheckoutViewModel
        {
            Cart = cart,
            UserAddresses = addresses
        };

        return View(viewModel);
    }

    [HttpPost]
    [Route("odeme/tamamla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(CheckoutViewModel model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Re-fetch cart (Calculated with Shipping)
        var cart = await _cartService.GetCartAsync(HttpContext);

        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Address handling
        Address shippingAddress = null;

        if (model.UseNewAddress)
        {
            // Validate new address
            if (string.IsNullOrWhiteSpace(model.NewAddress.Title) ||
            string.IsNullOrWhiteSpace(model.NewAddress.City) ||
            string.IsNullOrWhiteSpace(model.NewAddress.District) ||
            string.IsNullOrWhiteSpace(model.NewAddress.OpenAddress) ||
            string.IsNullOrWhiteSpace(model.NewAddress.FullName) ||
            string.IsNullOrWhiteSpace(model.NewAddress.Phone))
            {
                ModelState.AddModelError("", "Lütfen yeni adres bilgilerini eksiksiz doldurunuz.");
                model.Cart = cart;
                model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                return View("Index", model);
            }

            model.NewAddress.UserId = userId;
            _context.Addresses.Add(model.NewAddress);
            await _context.SaveChangesAsync();
            shippingAddress = model.NewAddress;
        }
        else
        {
            if (model.SelectedAddressId == 0)
            {
                ModelState.AddModelError("", "Lütfen bir teslimat adresi seçiniz.");
                model.Cart = cart;
                model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                return View("Index", model);
            }
            shippingAddress = await _context.Addresses.FindAsync(model.SelectedAddressId);
        }

        if (shippingAddress == null)
        {
            ModelState.AddModelError("", "Seçilen adres bulunamadı.");
            model.Cart = cart;
            model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            return View("Index", model);
        }

        // Billing Address Handling
        Address billingAddress = null;

        if (!model.UseDifferentBillingAddress)
        {
            billingAddress = shippingAddress;
        }
        else
        {
            if (model.UseNewBillingAddress)
            {
                // Validate new billing address
                if (string.IsNullOrWhiteSpace(model.NewBillingAddress.Title) ||
                    string.IsNullOrWhiteSpace(model.NewBillingAddress.City) ||
                    string.IsNullOrWhiteSpace(model.NewBillingAddress.District) ||
                    string.IsNullOrWhiteSpace(model.NewBillingAddress.OpenAddress) ||
                    string.IsNullOrWhiteSpace(model.NewBillingAddress.FullName) ||
                    string.IsNullOrWhiteSpace(model.NewBillingAddress.Phone))
                {
                    ModelState.AddModelError("", "Lütfen yeni fatura adresi bilgilerini eksiksiz doldurunuz.");
                    model.Cart = cart;
                    model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                    return View("Index", model);
                }

                model.NewBillingAddress.UserId = userId;
                model.NewBillingAddress.IsBillingAddress = true;
                _context.Addresses.Add(model.NewBillingAddress);
                await _context.SaveChangesAsync();
                billingAddress = model.NewBillingAddress;
            }
            else
            {
                if (model.SelectedBillingAddressId == 0)
                {
                    ModelState.AddModelError("", "Lütfen bir fatura adresi seçiniz.");
                    model.Cart = cart;
                    model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                    return View("Index", model);
                }
                billingAddress = await _context.Addresses.FindAsync(model.SelectedBillingAddressId);
            }
        }

        if (billingAddress == null)
        {
            ModelState.AddModelError("", "Seçilen fatura adresi bulunamadı.");
            model.Cart = cart;
            model.UserAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
            return View("Index", model);
        }

        // Create Order
        var order = new Order
        {
            UserId = userId,
            OrderNumber = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999),
            OrderDate = DateTime.Now,
            Status = OrderStatus.Pending,
            PaymentMethod = model.PaymentMethod == "BankTransfer" ? PaymentMethod.BankTransfer :
                            (model.PaymentMethod == "CreditCardIyzico" ? PaymentMethod.CreditCardIyzico : PaymentMethod.CreditCard),
            TotalAmount = cart.TotalAmount,
            // Kupon bilgileri (varsa)
            CouponCode = cart.Coupon?.Code,
            DiscountAmount = cart.DiscountAmount > 0 ? cart.DiscountAmount : null,
            SubTotalAmount = cart.DiscountAmount > 0 ? cart.SubTotal : null,
            // Teslimat adresi
            ShippingAddressTitle = shippingAddress.Title,
            ShippingAddressCity = shippingAddress.City,
            ShippingAddressDistrict = shippingAddress.District,
            ShippingAddressDetail = shippingAddress.OpenAddress,
            ShippingZipCode = shippingAddress.ZipCode,

            // Fatura Adresi
            BillingAddressTitle = billingAddress.Title,
            BillingAddressCity = billingAddress.City,
            BillingAddressDistrict = billingAddress.District,
            BillingAddressDetail = !string.IsNullOrEmpty(billingAddress.BillingAddress) ? billingAddress.BillingAddress : billingAddress.OpenAddress,
            BillingZipCode = billingAddress.ZipCode,
            BillingIdentityNumber = billingAddress.IdentityNumber,
            BillingCompanyName = billingAddress.CompanyName,
            BillingTaxOffice = billingAddress.TaxOffice,
            BillingTaxNumber = billingAddress.TaxNumber,

            // KDV ve Kargo Bilgileri
            VatAmount = cart.TotalVAT,
            SubTotalWithoutVat = cart.SubTotal,
            ShippingFee = cart.ShippingCost
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Create Order Items
        foreach (var item in cart.Items)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Price = item.Product.Price,
                Quantity = item.Quantity,
                VatRate = item.VatRate,
                VatAmount = item.VatAmount
            };
            _context.OrderItems.Add(orderItem);
        }

        // Clear Cart
        _context.CartItems.RemoveRange(cart.Items);
        cart.CouponId = null;
        _context.Update(cart);

        await _context.SaveChangesAsync();

        // Send Order Confirmation Email
        try
        {
            // Havale/EFT ile ödemelerde, ödeme onayı gelene kadar mail gönderme
            if (order.PaymentMethod != PaymentMethod.BankTransfer)
            {
                // Get user email
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    await _emailService.SendOrderConfirmationEmailAsync(userEmail, order);
                }
            }

            // Send Email to Admins
            var adminEmails = await _context.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.Email)
                .ToListAsync();

            if (adminEmails.Any())
            {
                await _emailService.SendEmailAsync(adminEmails, $"Yeni Sipariş: {order.OrderNumber}", $"Yeni bir sipariş alındı. Tutar: {order.TotalAmount:C2}. Sipariş No: {order.OrderNumber}");
            }
        }
        catch (Exception ex)
        {
            // Email sending failed, but order was successful. 
            // We might want to log this.
        }

        if (order.PaymentMethod == PaymentMethod.BankTransfer)
        {
            return RedirectToAction("BankTransferInfo", "Payment", new { orderId = order.Id });
        }
        else if (order.PaymentMethod == PaymentMethod.CreditCardIyzico)
        {
            return RedirectToAction("IyzicoPayment", "Payment", new { orderId = order.Id });
        }

        return RedirectToAction("PayTrPayment", "Payment", new { orderId = order.Id });
    }

    [Route("siparis-basarili")]
    public IActionResult Success(int orderId)
    {
        return View();
    }
}
