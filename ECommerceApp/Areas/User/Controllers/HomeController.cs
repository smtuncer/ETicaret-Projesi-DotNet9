using AutoMapper;
using ECommerceApp.Areas.User.ViewModels;
using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using ECommerceApp.Models.DTOs.Contact;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private const string ContactContentCacheKey = "contact-content-cache-key";

    public HomeController(
        ApplicationDbContext context,
        ILogger<HomeController> logger,
        IMemoryCache cache,
        IEmailService emailService,
        IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
        _emailService = emailService;
        _mapper = mapper;
    }

    [Route("/")]
    public async Task<IActionResult> Index()
    {
        var sliders = await _context.Sliders
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Order)
            .ToListAsync();

        var featuredProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedDate)
            .Take(10)
            .ToListAsync();

        var bestSellerProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity))
            .Take(10)
            .ToListAsync();

        var latestBlogs = await _context.Blogs
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.CreatedDate)
            .Take(4)
            .ToListAsync();



        var featuredCategories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && c.IsFeatured)
            .OrderBy(c => c.Order)
            .ToListAsync();

        var featuredCategoryProducts = new List<CategoryProductsDto>();

        foreach (var cat in featuredCategories)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .Where(p => p.IsActive && p.CategoryId == cat.Id)
                .OrderByDescending(p => p.CreatedDate)
                .Take(10)
                .ToListAsync();

            if (products.Any())
            {
                featuredCategoryProducts.Add(new CategoryProductsDto
                {
                    CategoryName = cat.Name,
                    CategoryUrl = Url.Action("Index", "Product", new { categoryId = cat.Id }),
                    Products = _mapper.Map<List<ECommerceApp.Models.DTOs.Product.ProductSummaryDto>>(products)
                });
            }
        }

        var viewModel = new HomeIndexViewModel
        {
            Sliders = sliders,
            FeaturedProducts = _mapper.Map<List<ECommerceApp.Models.DTOs.Product.ProductSummaryDto>>(featuredProducts),
            BestSellerProducts = _mapper.Map<List<ECommerceApp.Models.DTOs.Product.ProductSummaryDto>>(bestSellerProducts),
            LatestBlogs = latestBlogs,
            FeaturedCategoryProducts = featuredCategoryProducts
        };

        return View(viewModel);
    }

    [Route("/hakkimizda")]
    public async Task<IActionResult> AboutUs()
    {
        try
        {
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading about us page");
            return View("Error");
        }
    }

    [Route("/siparisimi-takip-et")]
    public async Task<IActionResult> OrderTracking(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return View();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Images)
            .Include(o => o.PaymentNotifications)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

        if (order == null)
        {
            ViewBag.ErrorMessage = "Girdiğiniz sipariş numarası ile eşleşen bir sipariş bulunamadı.";
            return View();
        }

        return View(order);
    }

    [Route("/banka-hesaplari")]
    public async Task<IActionResult> BankAccounts()
    {
        var accounts = await _context.BankAccounts
            .AsNoTracking()
            .Where(b => b.IsActive)
            .ToListAsync();

        return View(accounts);
    }

    [Route("/kurumsal-satis")]
    public async Task<IActionResult> CorporateSales()
    {
        var content = await _context.SiteContents
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        if (content == null)
            return View(new SiteContent { CorporateSalesTitle = "Kurumsal Satış", CorporateSalesContent = "İçerik hazırlanıyor..." });

        return View(content);
    }

    [Route("/iletisim")]
    public async Task<IActionResult> Contact()
    {
        var model = new ContactPageViewModel
        {
            ContactInfo = await GetContactInfoAsync(),
            Form = new ContactFormDto()
        };

        return View(model);
    }

    [HttpPost]
    [Route("/iletisim")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormDto form)
    {
        var contactInfo = await GetContactInfoAsync();
        var viewModel = new ContactPageViewModel
        {
            ContactInfo = contactInfo,
            Form = form
        };

        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            var entity = _mapper.Map<Models.ContactMessage>(form);
            entity.SentDate = DateTime.UtcNow;

            await _context.ContactMessages.AddAsync(entity);
            await _context.SaveChangesAsync();

            var recipient = !string.IsNullOrWhiteSpace(contactInfo.ContactEmail)
                ? contactInfo.ContactEmail
                : await _context.MailSettings
                    .AsNoTracking()
                    .Select(ms => ms.SenderEmail)
                    .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(recipient))
            {
                var body = $@"
                    <p><strong>Ad Soyad:</strong> {form.Name}</p>
                    <p><strong>E-posta:</strong> {form.Email}</p>
                    <p><strong>Konu:</strong> {form.Subject}</p>
                    <p><strong>Mesaj:</strong></p>
                    <p>{form.Message}</p>";

                await _emailService.SendEmailAsync(recipient, $"İletişim Formu: {form.Subject}", body);
            }

            viewModel.IsSuccess = true;
            viewModel.SuccessMessage = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
            viewModel.Form = new ContactFormDto();

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving contact message");
            ModelState.AddModelError(string.Empty, "Mesaj gönderilirken bir hata oluştu.");
            return View(viewModel);
        }
    }

    [Route("hata/{code}")]
    public IActionResult Error(int code)
    {
        return View(code);
    }

    private async Task<ContactInfoDto> GetContactInfoAsync()
    {
        if (_cache.TryGetValue(ContactContentCacheKey, out ContactInfoDto cached))
        {
            return cached;
        }

        var content = await _context.SiteContents
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

        var dto = content != null
            ? _mapper.Map<ContactInfoDto>(content)
            : new ContactInfoDto();

        _cache.Set(ContactContentCacheKey, dto, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30)
        });

        return dto;
    }
}