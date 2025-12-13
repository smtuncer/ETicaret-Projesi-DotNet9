using ECommerceApp.Helper;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

namespace ECommerceApp.Areas.User.Controllers
{
    [Area("User")]
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SitemapController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("/sitemap.xml")]
        [ResponseCache(Duration = 3600)] // Cache for 1 hour
        public async Task<IActionResult> Index()
        {
            // Use current request for base URL (http vs https, domain name)
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

            var root = new XElement(ns + "urlset");

            // 1. Static Pages
            var staticPages = new List<string>
            {
                "", // Home: /
                "/hakkimizda",
                "/iletisim",
                "/urunler", // Product Index
                "/blog"     // Blog Index
            };

            foreach (var page in staticPages)
            {
                // Ensure double slashes aren't created if page is empty or starts with /
                string relativeInfo = page;
                if (!string.IsNullOrEmpty(relativeInfo) && !relativeInfo.StartsWith("/")) relativeInfo = "/" + relativeInfo;

                string loc = string.IsNullOrEmpty(page) ? baseUrl + "/" : $"{baseUrl}{relativeInfo}";

                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", loc),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", page == "" ? "1.0" : "0.8")
                ));
            }

            // 2. Product Categories
            // Note: ProductController.Index uses categoryId query param.
            // Ideally should be a clean route, but we follow existing controller structure.
            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id })
                .AsNoTracking()
                .ToListAsync();

            foreach (var category in categories)
            {
                // Url.Action generates relative path.
                // Action: Index, Controller: Product, RouteValues: { categoryId = ... }
                // Result: /urunler?categoryId=...
                string url = Url.Action("Index", "Product", new { categoryId = category.Id });

                if (!string.IsNullOrEmpty(url))
                {
                    root.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}{url}"),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.8")
                    ));
                }
            }

            // 3. Products
            // Fetch only necessary fields to handle 10,000+ items efficiently
            var products = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.UpdatedDate
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var product in products)
            {
                // Use the helper extension method directly on IUrlHelper
                // This generates /urun/{slug}-{id}
                string relativeUrl = Url.ProductUrl(product.Id, product.Name, product.Slug);

                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}{relativeUrl}"),
                    new XElement(ns + "lastmod", product.UpdatedDate.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.9")
                ));
            }

            // 4. Blogs
            var blogs = await _context.Blogs
                .Where(b => b.IsPublished)
                .Select(b => new
                {
                    b.Id,
                    b.Slug,
                    b.UpdatedDate,
                    b.PublishedDate,
                    b.CreatedDate
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var blog in blogs)
            {
                // Route: /blog/{slug}-{id}
                string relativeUrl = Url.Action("Detail", "Blog", new { slug = blog.Slug, id = blog.Id });
                var date = blog.UpdatedDate ?? blog.PublishedDate ?? blog.CreatedDate;

                if (!string.IsNullOrEmpty(relativeUrl))
                {
                    root.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}{relativeUrl}"),
                        new XElement(ns + "lastmod", date.ToString("yyyy-MM-dd")),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.7")
                    ));
                }
            }

            // 5. Blog Categories
            // BlogController.Index filters by categoryId
            var blogCategories = await _context.BlogCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id })
                .AsNoTracking()
                .ToListAsync();

            foreach (var cat in blogCategories)
            {
                string url = Url.Action("Index", "Blog", new { categoryId = cat.Id });

                if (!string.IsNullOrEmpty(url))
                {
                    root.Add(new XElement(ns + "url",
                        new XElement(ns + "loc", $"{baseUrl}{url}"),
                        new XElement(ns + "changefreq", "weekly"),
                        new XElement(ns + "priority", "0.6")
                    ));
                }
            }

            return Content(new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
