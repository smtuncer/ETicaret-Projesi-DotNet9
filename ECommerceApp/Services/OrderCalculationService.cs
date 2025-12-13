using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services;

public interface IOrderCalculationService
{
    Task<OrderCalculation> CalculateOrderTotalsAsync(Cart cart);
    Task<SiteSettings> GetSiteSettingsAsync();
}

public class OrderCalculationService : IOrderCalculationService
{
    private readonly ApplicationDbContext _context;

    public OrderCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SiteSettings> GetSiteSettingsAsync()
    {
        var settings = await _context.SiteSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            // Varsayılan ayarları oluştur
            settings = new SiteSettings
            {
                VatRate = 20,
                ShippingFee = 30,
                FreeShippingThreshold = 500
            };
            _context.SiteSettings.Add(settings);
            await _context.SaveChangesAsync();
        }
        return settings;
    }

    public async Task<OrderCalculation> CalculateOrderTotalsAsync(Cart cart)
    {
        var settings = await GetSiteSettingsAsync();

        var calculation = new OrderCalculation();

        // 1. Ürünler toplamı (KDV Hariç)
        calculation.ProductsTotal = cart.SubTotal;

        // 2. İndirim tutarı
        calculation.DiscountAmount = cart.DiscountAmount;
        calculation.CouponCode = cart.Coupon?.Code;

        // 3. Ara toplam (İndirim sonrası, KDV hariç)
        calculation.SubTotalWithoutVat = calculation.ProductsTotal - calculation.DiscountAmount;

        // 4. KDV hesaplama
        calculation.VatRate = settings.VatRate;
        calculation.VatAmount = calculation.SubTotalWithoutVat * (settings.VatRate / 100);

        // 5. Kargo ücreti (ücretsiz kargo kontrolü)
        if (settings.FreeShippingThreshold.HasValue &&
            calculation.SubTotalWithoutVat >= settings.FreeShippingThreshold.Value)
        {
            calculation.ShippingFee = 0;
            calculation.IsFreeShipping = true;
        }
        else
        {
            calculation.ShippingFee = settings.ShippingFee;
            calculation.IsFreeShipping = false;
        }

        // 6. Genel toplam
        calculation.GrandTotal = calculation.SubTotalWithoutVat + calculation.VatAmount + calculation.ShippingFee;

        return calculation;
    }
}

public class OrderCalculation
{
    public decimal ProductsTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public decimal SubTotalWithoutVat { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public bool IsFreeShipping { get; set; }
    public decimal GrandTotal { get; set; }
}
