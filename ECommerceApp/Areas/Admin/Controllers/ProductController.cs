using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;

    public ProductController(ApplicationDbContext context, IToastNotification toast, IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _context = context;
        _toast = toast;
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<IActionResult> Index(string search, int? categoryId, int? brandId, bool? isFeatured, int page = 1)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        // Search
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search) || p.Barcode.Contains(search));
        }

        // Filter
        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId);
        }

        if (isFeatured.HasValue)
        {
            query = query.Where(p => p.IsFeatured == isFeatured);
        }

        // Pagination
        int pageSize = 10;
        int totalItems = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var products = await query
            .OrderByDescending(p => p.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.BrandId = brandId;
        ViewBag.IsFeatured = isFeatured;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;

        ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ImportXml(string source)
    {
        try
        {
            string xmlUrl1 = "http://xml.ebijuteri.com/api/xml/601b9ef80766d6212a27da02?format=old";
            string xmlUrl2 = "https://cdn1.xmlbankasi.com/p1/pjsymndiclkn/image/data/xml/urunler1.xml";
            string xmlUrl3 = "https://www.yntithalat.com/export/26f8870d6d673ff4f4eafafc408aec061h11AIIriSMyB7kFg==";
            string jobId = "";

            if (source == "ebijuteri")
            {
                jobId = Hangfire.BackgroundJob.Enqueue<IXmlImportService>(
                   service => service.ImportProductsAsync(xmlUrl1, null));
                _toast.AddInfoToastMessage("Ebijuteri XML içe aktarma işlemi arka planda başlatıldı.", new ToastrOptions { Title = "Bilgi" });
            }
            else if (source == "xmlbankasi")
            {
                jobId = Hangfire.BackgroundJob.Enqueue<IXmlImportService>(
                   service => service.ImportProductsAsync(xmlUrl2, null));
                _toast.AddInfoToastMessage("XmlBankasi XML içe aktarma işlemi arka planda başlatıldı.", new ToastrOptions { Title = "Bilgi" });
            }
            else if (source == "yntithalat")
            {
                jobId = Hangfire.BackgroundJob.Enqueue<IXmlImportService>(
                   service => service.ImportProductsAsync(xmlUrl3, null));
                _toast.AddInfoToastMessage("Yntİthalat XML içe aktarma işlemi arka planda başlatıldı.", new ToastrOptions { Title = "Bilgi" });
            }
            else
            {
                // Fallback (or both? The user wanted separate buttons, so presumably one at a time)
                // For safety, let's just show an error if unknown
                _toast.AddErrorToastMessage("Geçersiz XML kaynağı.", new ToastrOptions { Title = "Hata" });
                return RedirectToAction(nameof(Index));
            }

            // Store job ID in TempData to show status
            TempData["ImportJobId"] = jobId;
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("İçe aktarma başlatılırken bir hata oluştu: " + ex.Message, new ToastrOptions { Title = "Hata" });
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Attributes)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, List<string> AttributeNames, List<string> AttributeValues)
    {
        if (ModelState.IsValid)
        {
            product.CreatedDate = DateTime.Now;
            product.UpdatedDate = DateTime.Now;

            // Add Attributes
            if (AttributeNames != null && AttributeValues != null)
            {
                for (int i = 0; i < AttributeNames.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(AttributeNames[i]) && !string.IsNullOrWhiteSpace(AttributeValues[i]))
                    {
                        product.Attributes.Add(new ProductAttribute
                        {
                            Name = AttributeNames[i],
                            Value = AttributeValues[i]
                        });
                    }
                }
            }

            _context.Add(product);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Ürün başarıyla eklendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction(nameof(Index));
        }
        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
        return View(product);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products
            .Include(p => p.Attributes)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }
        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product, List<string> AttributeNames, List<string> AttributeValues)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (existingProduct == null) return NotFound();

                // Update basic fields
                existingProduct.Name = product.Name;
                existingProduct.SKU = product.SKU;
                existingProduct.ExternalId = product.ExternalId;
                existingProduct.Barcode = product.Barcode;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.DealerPrice = product.DealerPrice;
                existingProduct.Currency = product.Currency;
                existingProduct.VAT = product.VAT;
                existingProduct.Stock = product.Stock;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.IsActive = product.IsActive;
                existingProduct.IsFeatured = product.IsFeatured;
                existingProduct.UpdatedDate = DateTime.Now;

                // Update Attributes
                _context.ProductAttributes.RemoveRange(existingProduct.Attributes);
                if (AttributeNames != null && AttributeValues != null)
                {
                    for (int i = 0; i < AttributeNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(AttributeNames[i]) && !string.IsNullOrWhiteSpace(AttributeValues[i]))
                        {
                            existingProduct.Attributes.Add(new ProductAttribute
                            {
                                Name = AttributeNames[i],
                                Value = AttributeValues[i]
                            });
                        }
                    }
                }

                _context.Update(existingProduct);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Ürün güncellendi.", new ToastrOptions { Title = "Başarılı" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(product.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewData["BrandId"] = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
        return View(product);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Ürün silindi.", new ToastrOptions { Title = "Başarılı" });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .OrderBy(p => p.Id)
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Ürünler");

            // Headers
            worksheet.Cell(1, 1).Value = "Id";
            worksheet.Cell(1, 2).Value = "UrunAdi";
            worksheet.Cell(1, 3).Value = "SKU";
            worksheet.Cell(1, 4).Value = "Barkod";
            worksheet.Cell(1, 5).Value = "Fiyat";
            worksheet.Cell(1, 6).Value = "ParaBirimi";
            worksheet.Cell(1, 7).Value = "Stok";
            worksheet.Cell(1, 8).Value = "Kategori";
            worksheet.Cell(1, 9).Value = "Marka";
            worksheet.Cell(1, 10).Value = "Aciklama";
            worksheet.Cell(1, 11).Value = "Kdv";
            worksheet.Cell(1, 12).Value = "AktifMi";
            worksheet.Cell(1, 13).Value = "OneCikarilsinMi";

            // Data
            for (int i = 0; i < products.Count; i++)
            {
                var row = i + 2;
                var item = products[i];

                worksheet.Cell(row, 1).Value = item.Id;
                worksheet.Cell(row, 2).Value = item.Name;
                worksheet.Cell(row, 3).Value = item.SKU;
                worksheet.Cell(row, 4).Value = item.Barcode;
                worksheet.Cell(row, 5).Value = item.Price;
                worksheet.Cell(row, 6).Value = item.Currency;
                worksheet.Cell(row, 7).Value = item.Stock;
                worksheet.Cell(row, 8).Value = item.Category?.Name;
                worksheet.Cell(row, 9).Value = item.Brand?.Name;
                worksheet.Cell(row, 10).Value = item.Description?.Length > 32000 ? item.Description.Substring(0, 32000) : item.Description;
                worksheet.Cell(row, 11).Value = item.VAT;
                worksheet.Cell(row, 12).Value = item.IsActive ? "Evet" : "Hayır";
                worksheet.Cell(row, 13).Value = item.IsFeatured ? "Evet" : "Hayır";
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"urunler_{DateTime.Now:yyyyMMddHHmm}.xlsx");
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            _toast.AddErrorToastMessage("Lütfen bir Excel dosyası seçiniz.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream);
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header

                    // Cache for lookups
                    // Cache for lookups (Safe against duplicates)
                    var existingBrands = await _context.Brands.AsNoTracking().ToListAsync();
                    var brands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var b in existingBrands)
                    {
                        if (!brands.ContainsKey(b.Name)) brands[b.Name] = b.Id;
                    }

                    var existingCategories = await _context.Categories.AsNoTracking().ToListAsync();
                    var categories = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    foreach (var c in existingCategories)
                    {
                        if (!categories.ContainsKey(c.Name)) categories[c.Name] = c.Id;
                    }

                    int addedCount = 0;
                    int updatedCount = 0;

                    foreach (var row in rows)
                    {
                        // Parse Id
                        var idVal = row.Cell(1).GetValue<string>();
                        int.TryParse(idVal, out int id);

                        Product product;
                        if (id > 0)
                        {
                            product = await _context.Products.Include(p => p.Attributes).FirstOrDefaultAsync(p => p.Id == id);
                            if (product == null)
                            {
                                // If ID provided but not found, create new? Or skip?
                                // Let's treat as accessible create or just skip. Usually ID implies update.
                                // Let's skip to be safe against overwriting wrong things if IDs don't match db.
                                continue;
                            }
                        }
                        else
                        {
                            product = new Product { CreatedDate = DateTime.Now };
                        }

                        // Map fields
                        product.Name = row.Cell(2).GetValue<string>();
                        product.SKU = row.Cell(3).GetValue<string>();
                        product.Barcode = row.Cell(4).GetValue<string>();

                        var priceVal = row.Cell(5).GetValue<string>();
                        if (decimal.TryParse(priceVal, out decimal price)) product.Price = price;

                        product.Currency = row.Cell(6).GetValue<string>();

                        var stockVal = row.Cell(7).GetValue<string>();
                        if (int.TryParse(stockVal, out int stock)) product.Stock = stock;

                        // Category
                        var catName = row.Cell(8).GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(catName))
                        {
                            if (categories.TryGetValue(catName, out int catId))
                            {
                                product.CategoryId = catId;
                            }
                            else
                            {
                                // Create Category?
                                var newCat = new Category { Name = catName, IsActive = true };
                                _context.Categories.Add(newCat);
                                await _context.SaveChangesAsync();
                                categories[catName] = newCat.Id;
                                product.CategoryId = newCat.Id;
                            }
                        }

                        // Brand
                        var brandName = row.Cell(9).GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(brandName))
                        {
                            if (brands.TryGetValue(brandName, out int brandId))
                            {
                                product.BrandId = brandId;
                            }
                            else
                            {
                                // Create Brand?
                                var newBrand = new Brand { Name = brandName, IsActive = true };
                                _context.Brands.Add(newBrand);
                                await _context.SaveChangesAsync();
                                brands[brandName] = newBrand.Id;
                                product.BrandId = newBrand.Id;
                            }
                        }

                        product.Description = row.Cell(10).GetValue<string>();

                        var vatVal = row.Cell(11).GetValue<string>();
                        if (int.TryParse(vatVal, out int vat)) product.VAT = vat;

                        var activeVal = row.Cell(12).GetValue<string>();
                        product.IsActive = activeVal?.Equals("Evet", StringComparison.OrdinalIgnoreCase) == true;

                        var featuredVal = row.Cell(13).GetValue<string>();
                        product.IsFeatured = featuredVal?.Equals("Evet", StringComparison.OrdinalIgnoreCase) == true;

                        product.UpdatedDate = DateTime.Now;

                        if (id == 0)
                        {
                            _context.Products.Add(product);
                            addedCount++;
                        }
                        else
                        {
                            _context.Update(product);
                            updatedCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    _toast.AddSuccessToastMessage($"Excel içe aktarımı tamamlandı. {addedCount} yeni ürün, {updatedCount} güncellenen ürün.", new ToastrOptions { Title = "Başarılı" });
                }
            }
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Excel işlenirken hata oluştu: " + ex.Message, new ToastrOptions { Title = "Hata" });
        }

        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }
}
