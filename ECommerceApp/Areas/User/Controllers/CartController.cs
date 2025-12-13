using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Globalization;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toastNotification;
    private readonly ICartService _cartService;

    public CartController(ApplicationDbContext context, IToastNotification toastNotification, ICartService cartService)
    {
        _context = context;
        _toastNotification = toastNotification;
        _cartService = cartService;
    }

    [Route("sepet")]
    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(HttpContext);
        return View(cart);
    }

    [HttpPost]
    [Route("sepet/ekle")]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        await _cartService.AddToCartAsync(HttpContext, productId, quantity);
        _toastNotification.AddSuccessToastMessage("Ürün sepete eklendi!", new ToastrOptions { Title = "Başarılı" });
        return Json(new { success = true, message = "Ürün sepete eklendi." });
    }

    [HttpPost]
    [Route("sepet/sil")]
    public async Task<IActionResult> RemoveFromCart(int itemId)
    {
        await _cartService.RemoveFromCartAsync(HttpContext, itemId);
        return Json(new { success = true, message = "Ürün sepetten silindi." });
    }

    [HttpPost]
    [Route("sepet/guncelle")]
    public async Task<IActionResult> UpdateQuantity(int itemId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(HttpContext, itemId, quantity);
        return Json(new { success = true, message = quantity > 0 ? "Adet güncellendi." : "Ürün sepetten silindi." });
    }

    [HttpGet]
    [Route("sepet/ozet")]
    public async Task<IActionResult> GetCartPreview()
    {
        var cart = await _cartService.GetCartAsync(HttpContext);
        return PartialView("_CartPreview", cart);
    }

    [HttpPost]
    [Authorize]
    [Route("sepet/kupon-uygula")]
    public async Task<IActionResult> ApplyCoupon(string code)
    {
        // Use CartService to get the calculated cart
        var cart = await _cartService.GetCartAsync(HttpContext);

        if (cart == null)
        {
            return Json(new { success = false, message = "Sepet bulunamadı." });
        }

        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == code.Trim() && c.IsActive);

        if (coupon == null)
        {
            return Json(new { success = false, message = "Geçersiz kupon kodu." });
        }

        if (DateTime.Now < coupon.StartDate || DateTime.Now > coupon.EndDate)
        {
            return Json(new { success = false, message = "Kuponun süresi dolmuş veya henüz başlamamış." });
        }
        var amountText = coupon.MinCartAmount.Value.ToString("C2", new CultureInfo("tr-TR"));

        // Check against Total Amount (SubTotal + VAT) as per requirement
        var cartTotalWithVat = cart.SubTotal + cart.TotalVAT;

        if (coupon.MinCartAmount.HasValue && cartTotalWithVat < coupon.MinCartAmount.Value)
        {
            return Json(new { success = false, message = $"Bu kuponu kullanmak için sepet tutarı (KDV Dahil) en az {amountText} olmalıdır." });
        }

        cart.CouponId = coupon.Id;
        _context.Update(cart);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Kupon başarıyla uygulandı." });
    }

    [HttpPost]
    [Authorize]
    [Route("sepet/kupon-kaldir")]
    public async Task<IActionResult> RemoveCoupon()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return Json(new { success = false, message = "Sepet bulunamadı." });
        }

        cart.CouponId = null;
        _context.Update(cart);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Kupon kaldırıldı." });
    }
}
