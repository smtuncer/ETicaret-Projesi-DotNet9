using ECommerceApp.Helper;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Xml.Linq;

namespace ECommerceApp.Areas.User.Controllers
{
    [Area("User")]
    public class FeedController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public FeedController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        [Route("/feed/akakce.xml")]
        public async Task<IActionResult> Akakce()
        {
            if (_cache.TryGetValue("Feed_Akakce", out string cachedXml))
            {
                return Content(cachedXml, "application/xml", Encoding.UTF8);
            }

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            // Fetch only necessary data
            var products = await _context.Products
               .Where(p => p.IsActive && p.Stock > 2)
               .AsNoTracking()
               .Select(p => new
               {
                   p.Id,
                   p.Name,
                   p.Slug,
                   p.Price,
                   p.Currency,
                   p.Stock,
                   p.Description,
                   p.Barcode,
                   BrandName = p.Brand.Name ?? "",
                   CategoryName = p.Category.Name ?? "",
                   MainImage = p.Images.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsMain) ?? p.Images.FirstOrDefault()
               })
               .ToListAsync();

            var settings = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
            decimal globalShippingFee = settings?.ShippingFee ?? 0;
            decimal freeShippingThreshold = settings?.FreeShippingThreshold ?? 0;

            var root = new XElement("site");

            foreach (var p in products)
            {
                string productUrl = baseUrl + Url.ProductUrl(p.Id, p.Name, p.Slug);
                string imageUrl = "";

                if (p.MainImage != null)
                {
                    imageUrl = p.MainImage.ImageUrl.StartsWith("http") ? p.MainImage.ImageUrl : baseUrl + p.MainImage.ImageUrl;
                }

                decimal productShippingPrice = (p.Price >= freeShippingThreshold && freeShippingThreshold > 0) ? 0 : globalShippingFee;

                var item = new XElement("product",
                    new XElement("id", p.Id),
                    new XElement("name", p.Name),
                    new XElement("price", p.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement("currencyType", p.Currency ?? "TL"),
                    new XElement("url", productUrl),
                    new XElement("image", imageUrl),
                    new XElement("stock", p.Stock),
                    new XElement("brand", p.BrandName),
                    new XElement("category", p.CategoryName),
                    new XElement("ean", p.Barcode ?? ""),
                    new XElement("description", new XCData(p.Description ?? "")),
                    new XElement("shipping_fee", productShippingPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                );

                root.Add(item);
            }

            string xmlOutput = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();

            _cache.Set("Feed_Akakce", xmlOutput, TimeSpan.FromMinutes(60));

            return Content(xmlOutput, "application/xml", Encoding.UTF8);
        }

        [Route("/feed/cimri.xml")]
        public async Task<IActionResult> Cimri()
        {
            if (_cache.TryGetValue("Feed_Cimri", out string cachedXml))
            {
                return Content(cachedXml, "application/xml", Encoding.UTF8);
            }

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = await _context.Products
               .Where(p => p.IsActive && p.Stock > 2)
               .AsNoTracking()
               .Select(p => new
               {
                   p.Id,
                   p.Name,
                   p.Slug,
                   p.Price,
                   p.Stock,
                   p.Description,
                   BrandName = p.Brand.Name ?? "",
                   CategoryName = p.Category.Name ?? "",
                   MainImage = p.Images.OrderBy(x => x.SortOrder).FirstOrDefault(x => x.IsMain) ?? p.Images.FirstOrDefault()
               })
               .ToListAsync();

            var settings = await _context.SiteSettings.AsNoTracking().FirstOrDefaultAsync();
            decimal globalShippingFee = settings?.ShippingFee ?? 0;
            decimal freeShippingThreshold = settings?.FreeShippingThreshold ?? 0;

            var root = new XElement("MerchantItems");

            foreach (var p in products)
            {
                string productUrl = baseUrl + Url.ProductUrl(p.Id, p.Name, p.Slug);
                string imageUrl = "";

                if (p.MainImage != null)
                {
                    imageUrl = p.MainImage.ImageUrl.StartsWith("http") ? p.MainImage.ImageUrl : baseUrl + p.MainImage.ImageUrl;
                }

                decimal productShippingPrice = (p.Price >= freeShippingThreshold && freeShippingThreshold > 0) ? 0 : globalShippingFee;

                var item = new XElement("MerchantItem",
                    new XElement("merchantItemId", p.Id),
                    new XElement("itemTitle", p.Name),
                    new XElement("itemCategory", p.CategoryName),
                    new XElement("itemBrand", p.BrandName),
                    new XElement("itemPrice", p.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement("itemUrl", productUrl),
                    new XElement("itemImageUrl", imageUrl),
                    new XElement("stockAmount", p.Stock),
                    new XElement("shippingFee", productShippingPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                    new XElement("itemDescription", new XCData(p.Description ?? ""))
                );

                root.Add(item);
            }

            string xmlOutput = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();

            _cache.Set("Feed_Cimri", xmlOutput, TimeSpan.FromMinutes(60));

            return Content(xmlOutput, "application/xml", Encoding.UTF8);
        }
    }
}
