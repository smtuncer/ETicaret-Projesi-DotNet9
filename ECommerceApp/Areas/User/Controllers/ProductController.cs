using AutoMapper;
using ECommerceApp.Areas.User.ViewModels;
using ECommerceApp.Helper;
using ECommerceApp.Models.Data;
using ECommerceApp.Models.DTOs.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ApplicationDbContext context, IMemoryCache cache, IMapper mapper, ILogger<ProductController> logger)
    {
        _context = context;
        _cache = cache;
        _mapper = mapper;
        _logger = logger;
    }

    [Route("urunler")]
    [Route("urunler/sayfa-{page:int}")]
    [Route("kategori/{categorySlug}-{categoryId:int}")]
    [Route("kategori/{categorySlug}-{categoryId:int}/sayfa-{page:int}")]
    public async Task<IActionResult> Index(int? categoryId, string categorySlug, List<int> brandIds, string search, decimal? minPrice, decimal? maxPrice, string sort, int page = 1)
    {
        int pageSize = 12;
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Comments)
            .Where(p => p.IsActive);

        string selectedCategoryName = null;
        List<int> categoryIds = new List<int>();

        // Filter by Category
        if (categoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(categoryId.Value);
            if (category != null)
            {
                selectedCategoryName = category.Name;
            }

            // Include subcategories
            categoryIds = await GetCategoryIdsAsync(categoryId.Value);
            query = query.Where(p => categoryIds.Contains(p.CategoryId.Value));
        }

        // Filter by Brand
        if (brandIds != null && brandIds.Any())
        {
            query = query.Where(p => p.BrandId.HasValue && brandIds.Contains(p.BrandId.Value));
        }

        // Filter by Search
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
        }

        // Filter by Price
        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }
        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        // Önce öne çıkarılan ürünleri getir
        IOrderedQueryable<Models.Product> orderedQuery = query.OrderByDescending(p => p.IsFeatured);

        // Sorting (öne çıkarma önceliğini bozmadan)
        switch (sort)
        {
            case "price_asc":
                orderedQuery = orderedQuery.ThenBy(p => p.Price);
                break;
            case "price_desc":
                orderedQuery = orderedQuery.ThenByDescending(p => p.Price);
                break;
            case "newest":
                orderedQuery = orderedQuery.ThenByDescending(p => p.CreatedDate);
                break;
            case "rating":
                orderedQuery = orderedQuery.ThenByDescending(p => p.Comments.Where(c => c.IsApproved).Select(c => (double?)c.Rating).Average() ?? 0);
                break;
            case "bestseller":
                orderedQuery = orderedQuery.ThenByDescending(p => p.OrderItems.Sum(oi => oi.Quantity));
                break;
            default: // varsayılan: en yeni
                orderedQuery = orderedQuery.ThenByDescending(p => p.CreatedDate);
                break;
        }

        var totalCount = await orderedQuery.CountAsync();
        var products = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 1. Fetch all active categories (AsNoTracking for performance)
        var allActiveCategories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name)
            .ToListAsync();

        // 2. Build Hierarchy in Memory
        var categoryLookup = allActiveCategories.ToDictionary(c => c.Id);
        var rootCategories = new List<Models.Category>();

        foreach (var cat in allActiveCategories)
        {
            cat.SubCategories = new List<Models.Category>();
        }

        foreach (var cat in allActiveCategories)
        {
            if (cat.ParentId.HasValue && categoryLookup.TryGetValue(cat.ParentId.Value, out var parent))
            {
                parent.SubCategories.Add(cat);
            }
            else
            {
                rootCategories.Add(cat);
            }
        }

        var model = new ProductListViewModel
        {
            Products = products,
            Categories = rootCategories,
            Brands = await (categoryId.HasValue
                ? _context.Products
                    .Where(p => p.IsActive && categoryIds.Contains(p.CategoryId.Value) && p.BrandId.HasValue)
                    .Select(p => p.Brand)
                    .Distinct()
                    .OrderBy(b => b.Name)
                    .ToListAsync()
                : _context.Brands
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync()),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            SelectedCategoryId = categoryId,
            SelectedCategoryName = selectedCategoryName,
            SelectedBrandIds = brandIds ?? new List<int>(),
            SearchTerm = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortOrder = sort,
            TotalCount = totalCount
        };

        return View(model);
    }

    [Route("one-cikan-urunler")]
    [Route("one-cikan-urunler/sayfa-{page:int}")]
    public async Task<IActionResult> Features(int page = 1)
    {
        const int pageSize = 12;

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.CreatedDate);

        var totalCount = await query.CountAsync();
        var products = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new FeaturedProductsViewModel
        {
            Products = _mapper.Map<List<ProductSummaryDto>>(products),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalCount = totalCount
        };

        return View(model);
    }

    private async Task<List<int>> GetCategoryIdsAsync(int parentId)
    {
        var ids = new List<int> { parentId };
        var subCategories = await _context.Categories.Where(c => c.ParentId == parentId).Select(c => c.Id).ToListAsync();
        foreach (var subId in subCategories)
        {
            ids.AddRange(await GetCategoryIdsAsync(subId));
        }
        return ids;
    }

    [Route("urun/{slug}-{id:int}")]
    public async Task<IActionResult> Detail(string slug, int id)
    {
        try
        {
            var cacheKey = $"product-detail-{id}";
            if (!_cache.TryGetValue(cacheKey, out ProductDetailViewModel? cachedViewModel))
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Images)
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.IsActive && p.Id == id);

                if (product == null)
                {
                    return NotFound();
                }

                // Generate slug if it doesn't exist
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = Url.FriendlyURLTitle(product.Name);

                    // Update product with generated slug (for future use)
                    // We need a separate context or tracked entity to update
                    var productToUpdate = await _context.Products.FindAsync(id);
                    if (productToUpdate != null)
                    {
                        productToUpdate.Slug = product.Slug;
                        await _context.SaveChangesAsync();
                    }
                }

                var relatedProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Images)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive && p.CategoryId == product.CategoryId && p.Id != product.Id)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(2)
                    .ToListAsync();

                var featuredProducts = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Images)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive && p.IsFeatured && p.Id != product.Id)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                var approvedComments = await _context.ProductComments
                    .Where(c => c.ProductId == id && c.IsApproved)
                    .OrderByDescending(c => c.CreatedDate)
                    .ToListAsync();

                cachedViewModel = new ProductDetailViewModel
                {
                    Product = _mapper.Map<ProductDetailDto>(product),
                    ImageUrls = product.Images
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .ToList(),
                    Attributes = _mapper.Map<List<ProductAttributeDto>>(product.Attributes
                        .OrderBy(a => a.Name)),
                    RelatedProducts = _mapper.Map<List<ProductSummaryDto>>(relatedProducts),
                    FeaturedProducts = _mapper.Map<List<ProductSummaryDto>>(featuredProducts),
                    Comments = approvedComments,
                    TotalReviews = approvedComments.Count,
                    AverageRating = approvedComments.Any() ? approvedComments.Average(c => c.Rating) : 0
                };

                if (!cachedViewModel.ImageUrls.Any())
                {
                    cachedViewModel.ImageUrls = new List<string> { "/martfury/img/products/shop/1.jpg" };
                }

                _cache.Set(cacheKey, cachedViewModel, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
            }

            if (cachedViewModel == null)
            {
                return NotFound();
            }

            // Create a fresh instance for the current request to avoid mutating the cached object for specific user data
            var viewModel = new ProductDetailViewModel
            {
                Product = cachedViewModel.Product,
                ImageUrls = cachedViewModel.ImageUrls,
                Attributes = cachedViewModel.Attributes,
                RelatedProducts = cachedViewModel.RelatedProducts,
                FeaturedProducts = cachedViewModel.FeaturedProducts,
                Comments = cachedViewModel.Comments,
                AverageRating = cachedViewModel.AverageRating,
                TotalReviews = cachedViewModel.TotalReviews
            };


            // Check if user purchased the product
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                viewModel.UserHasPurchased = await _context.Orders
                    .AsNoTracking()
                    .AnyAsync(o => o.UserId == userId && o.Items.Any(i => i.ProductId == id));

                viewModel.IsFavored = await _context.Favorites
                    .AsNoTracking()
                    .AnyAsync(f => f.UserId == userId && f.ProductId == id);
            }

            // Redirect to correct slug if needed
            if (!slug.Equals(viewModel.Product.Slug, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToActionPermanent("Detail", new { slug = viewModel.Product.Slug, id = viewModel.Product.Id });
            }

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürün detayı yüklenirken hata meydana geldi. ProductId: {ProductId}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int productId, string name, string email, string comment, int rating)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
            {
                userId = uid;
            }

            string urlSlug = !string.IsNullOrEmpty(product.Slug) ? product.Slug : Url.FriendlyURLTitle(product.Name);

            if (userId == null)
            {
                TempData["CommentError"] = "Yorum yapmak için giriş yapmalısınız.";
                return RedirectToAction("Detail", new { slug = urlSlug, id = product.Id });
            }

            // Check if user purchased the product
            bool hasPurchased = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.Items.Any(i => i.ProductId == productId));

            if (!hasPurchased)
            {
                TempData["CommentError"] = "Bu ürünü değerlendirmek için satın almış olmanız gerekmektedir.";
                return RedirectToAction("Detail", new { slug = urlSlug, id = product.Id });
            }

            var productComment = new Models.ProductComment
            {
                ProductId = productId,
                UserId = userId,
                Name = name,
                Email = email,
                Comment = comment,
                Rating = rating,
                IsApproved = false, // Requires admin approval
                CreatedDate = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _context.ProductComments.AddAsync(productComment);
            await _context.SaveChangesAsync();

            TempData["CommentSuccess"] = "Yorumunuz başarıyla gönderildi. Onaylandıktan sonra yayınlanacaktır.";

            // Redirect back to detail with slug
            return RedirectToAction("Detail", new { slug = urlSlug, id = product.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yorum eklenirken hata: {ProductId}", productId);
            TempData["CommentError"] = "Yorum gönderilirken bir hata oluştu.";
            return RedirectToAction("Detail", new { slug = "urun", id = productId });
        }
    }

    [HttpGet]
    [Route("api/products/search")]
    public async Task<IActionResult> SearchAutocomplete(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return Json(new { results = new List<object>() });
        }

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Brand)
            .Where(p => p.IsActive && p.Name.Contains(term))
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedDate)
            .Take(8)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                price = p.Price,
                brand = p.Brand != null ? p.Brand.Name : "",
                image = p.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).FirstOrDefault() ?? "/martfury/img/products/shop/1.jpg"
            })
            .ToListAsync();

        return Json(new { results = products });
    }
}
