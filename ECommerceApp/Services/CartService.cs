using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ECommerceApp.Services;

public interface ICartService
{
    Task<Cart> GetCartAsync(HttpContext httpContext);
    Task AddToCartAsync(HttpContext httpContext, int productId, int quantity = 1);
    Task RemoveFromCartAsync(HttpContext httpContext, int itemId);
    Task UpdateQuantityAsync(HttpContext httpContext, int itemId, int quantity);
    Task MergeSessionCartToUserCartAsync(HttpContext httpContext, int userId);
    Task<int> GetCartItemCountAsync(HttpContext httpContext);
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private const string CartSessionKey = "GuestCart";

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart> GetCartAsync(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            // Kullanıcı giriş yapmış - veritabanından al
            var userId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .ThenInclude(p => p.Images)
                .Include(c => c.Coupon)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            await CalculateCartTotalsAsync(cart);
            return cart;
        }
        else
        {
            // Misafir kullanıcı - session'dan al
            var sessionCart = httpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(sessionCart))
            {
                return new Cart { Items = new List<CartItem>() };
            }

            var cart = JsonSerializer.Deserialize<Cart>(sessionCart);

            // Ürün bilgilerini veritabanından çek
            if (cart != null && cart.Items.Any())
            {
                var productIds = cart.Items.Select(i => i.ProductId).ToList();
                var products = await _context.Products
                    .Include(p => p.Images)
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var item in cart.Items)
                {
                    item.Product = products.FirstOrDefault(p => p.Id == item.ProductId);
                }
            }

            var resultCart = cart ?? new Cart { Items = new List<CartItem>() };
            await CalculateCartTotalsAsync(resultCart);
            return resultCart;
        }
    }

    public async Task AddToCartAsync(HttpContext httpContext, int productId, int quantity = 1)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            // Kullanıcı giriş yapmış - veritabanına ekle
            var userId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            // Misafir kullanıcı - session'a ekle
            var cart = await GetCartAsync(httpContext);

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                var product = await _context.Products.FindAsync(productId);
                if (product != null)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        Product = product
                    });
                }
            }

            SaveCartToSession(httpContext, cart);
        }
    }

    public async Task RemoveFromCartAsync(HttpContext httpContext, int itemId)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartAsync(httpContext);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == itemId);
            if (item != null)
            {
                cart.Items.Remove(item);
                SaveCartToSession(httpContext, cart);
            }
        }
    }

    public async Task UpdateQuantityAsync(HttpContext httpContext, int itemId, int quantity)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = int.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var cartItem = await _context.CartItems
                .Include(i => i.Cart)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Cart.UserId == userId);

            if (cartItem != null)
            {
                if (quantity > 0)
                {
                    cartItem.Quantity = quantity;
                    _context.Update(cartItem);
                }
                else
                {
                    _context.CartItems.Remove(cartItem);
                }
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var cart = await GetCartAsync(httpContext);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == itemId);
            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.Items.Remove(item);
                }
                SaveCartToSession(httpContext, cart);
            }
        }
    }

    public async Task MergeSessionCartToUserCartAsync(HttpContext httpContext, int userId)
    {
        var sessionCartJson = httpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(sessionCartJson))
        {
            return;
        }

        var sessionCart = JsonSerializer.Deserialize<Cart>(sessionCartJson);
        if (sessionCart == null || !sessionCart.Items.Any())
        {
            return;
        }

        var userCart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (userCart == null)
        {
            userCart = new Cart { UserId = userId };
            _context.Carts.Add(userCart);
            await _context.SaveChangesAsync();
        }

        foreach (var sessionItem in sessionCart.Items)
        {
            var existingItem = userCart.Items.FirstOrDefault(i => i.ProductId == sessionItem.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += sessionItem.Quantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = userCart.Id,
                    ProductId = sessionItem.ProductId,
                    Quantity = sessionItem.Quantity
                });
            }
        }

        await _context.SaveChangesAsync();

        // Session cart'ı temizle
        httpContext.Session.Remove(CartSessionKey);
    }

    public async Task<int> GetCartItemCountAsync(HttpContext httpContext)
    {
        var cart = await GetCartAsync(httpContext);
        return cart.Items.Sum(i => i.Quantity);
    }

    public async Task CalculateCartTotalsAsync(Cart cart)
    {
        if (cart == null) return;

        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            // Default settings if none exist
            settings = new SiteSettings();
        }

        decimal subTotal = 0;
        decimal totalVat = 0;

        foreach (var item in cart.Items)
        {
            if (item.Product != null)
            {
                // Determine VAT Rate: Use Product's VAT if > 0, otherwise Global Setting
                decimal vatRate = (item.Product.VAT > 0) ? item.Product.VAT : settings.VatRate;

                // Calculate VAT Amount per unit
                decimal vatAmountPerUnit = item.Product.Price * (vatRate / 100);

                // Set Item properties for display
                item.VatRate = vatRate;
                item.VatAmount = vatAmountPerUnit * item.Quantity;
                item.PriceWithVat = item.Product.Price + vatAmountPerUnit;

                // Accumulate Totals
                subTotal += item.Quantity * item.Product.Price;
                totalVat += item.VatAmount;
            }
        }

        cart.SubTotal = subTotal;
        cart.TotalVAT = totalVat;

        // Calculate Discount
        decimal discountAmount = 0;
        if (cart.Coupon != null && cart.Coupon.IsActive && DateTime.Now >= cart.Coupon.StartDate && DateTime.Now <= cart.Coupon.EndDate)
        {
            decimal cartTotalWithVat = cart.SubTotal + cart.TotalVAT;
            if (!cart.Coupon.MinCartAmount.HasValue || cartTotalWithVat >= cart.Coupon.MinCartAmount.Value)
            {
                if (cart.Coupon.DiscountType == DiscountType.Percentage)
                {
                    discountAmount = cart.SubTotal * (cart.Coupon.DiscountValue / 100);
                }
                else
                {
                    discountAmount = cart.Coupon.DiscountValue;
                }
            }
        }
        cart.DiscountAmount = discountAmount;

        // Calculate Shipping
        // "Sepette ki kdvli toplam tutar" refers to (SubTotal + TotalVAT) - Discount? Or just SubTotal+VAT?
        // Usually Free Shipping threshold is based on the Final Payable Amount or the Order Value.
        // Let's assume (SubTotal + TotalVAT) - DiscountAmount is the "Cart Total" relevant for payment, but usually threshold is on Product Value.
        // User said: "Sepette ki kdvli toplam tutar eğer ücretsiz kargo limitimin altındaysa"
        // Meaning: (SubTotal + TotalVAT) < Threshold

        decimal totalWithVat = cart.SubTotal + cart.TotalVAT;

        if (!cart.Items.Any())
        {
            cart.ShippingCost = 0;
        }
        else if (settings.FreeShippingThreshold.HasValue && totalWithVat >= settings.FreeShippingThreshold.Value)
        {
            cart.ShippingCost = 0;
        }
        else
        {
            cart.ShippingCost = settings.ShippingFee;
        }

        // Grand Total
        cart.TotalAmount = Math.Max(0, totalWithVat + cart.ShippingCost - cart.DiscountAmount);
    }

    private void SaveCartToSession(HttpContext httpContext, Cart cart)
    {
        // Circular reference'ları önlemek için sadece gerekli bilgileri kaydet
        var sessionCart = new Cart
        {
            Items = cart.Items.Select(i => new CartItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        var json = JsonSerializer.Serialize(sessionCart);
        httpContext.Session.SetString(CartSessionKey, json);
    }
}
