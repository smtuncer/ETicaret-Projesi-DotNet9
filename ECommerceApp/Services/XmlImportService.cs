using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Xml.Linq;

namespace ECommerceApp.Services;

public class XmlImportService : IXmlImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<XmlImportService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public XmlImportService(ApplicationDbContext context, ILogger<XmlImportService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task ImportAllProductsAsync()
    {
        string xmlUrl1 = "http://xml.ebijuteri.com/api/xml/601b9ef80766d6212a27da02?format=old";
        string xmlUrl2 = "https://cdn1.xmlbankasi.com/p1/pjsymndiclkn/image/data/xml/urunler1.xml";
        string xmlUrl3 = "https://www.yntithalat.com/export/26f8870d6d673ff4f4eafafc408aec061h11AIIriSMyB7kFg==";

        try
        {
            await ImportProductsAsync(xmlUrl1, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing Ebijuteri in daily job");
        }

        try
        {
            await ImportProductsAsync(xmlUrl2, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing XmlBankasi in daily job");
        }

        try
        {
            await ImportProductsAsync(xmlUrl3, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing YntIthalat in daily job");
        }
    }

    public async Task ImportProductsAsync(string xmlUrl, IProgress<int>? progress)
    {
        try
        {
            _logger.LogInformation("Starting XML import from {Url}", xmlUrl);

            // 1. Download and parse XML
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(10);
            var response = await client.GetStringAsync(xmlUrl);
            var xdoc = XDocument.Parse(response);

            // Determine Provider Type
            bool isEbijuteri = xmlUrl.Contains("ebijuteri.com");
            bool isYntIthalat = xmlUrl.Contains("yntithalat.com");

            // Should match what we expect. Ebijuteri uses <Urun>, XmlBankasi uses <Product>, Ynt uses <product>
            List<XElement> products;
            if (isEbijuteri)
            {
                products = xdoc.Descendants("Urun").ToList();
            }
            else if (isYntIthalat)
            {
                // Using lowercase 'product' based on user sample
                products = xdoc.Descendants("product").ToList();
                // If empty, try Title case just in case
                if (!products.Any()) products = xdoc.Descendants("Product").ToList();
            }
            else
            {
                products = xdoc.Descendants("Product").ToList();
            }

            int total = products.Count;
            _logger.LogInformation("Found {Count} products in XML for provider: {Provider}", total,
                isEbijuteri ? "Ebijuteri" : (isYntIthalat ? "YntIthalat" : "XmlBankasi"));

            // 2. Pre-load existing data lookups
            var existingProductExternalIds = await _context.Products
                .AsNoTracking()
                .Where(p => p.ExternalId != null)
                .Select(p => p.ExternalId)
                .ToListAsync();
            var existingProductSet = new HashSet<string>(existingProductExternalIds!);

            var existingBrands = await _context.Brands.ToListAsync();
            var brandDict = existingBrands.ToDictionary(b => b.Name, b => b.Id, StringComparer.OrdinalIgnoreCase);

            var existingCategories = await _context.Categories.ToListAsync();
            var categoryPathCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // 3. Process in batches
            const int batchSize = 100;
            int processed = 0;

            // Global cache for this session
            var categoryCache = existingCategories
                .Select(c => new { Key = $"{(c.ParentId ?? 0)}|{c.Name.Trim()}", Value = c.Id })
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < products.Count; i += batchSize)
            {
                var batch = products.Skip(i).Take(batchSize).ToList();

                if (isEbijuteri)
                {
                    await ProcessBatchEbijuteriAsync(batch, existingProductSet, brandDict, categoryCache);
                }
                else if (isYntIthalat)
                {
                    await ProcessBatchYntIthalatAsync(batch, existingProductSet, brandDict, categoryCache);
                }
                else
                {
                    await ProcessBatchXmlBankasiAsync(batch, existingProductSet, brandDict, categoryCache);
                }

                processed += batch.Count;
                int percent = total > 0 ? (int)((double)processed / total * 100) : 0;
                progress?.Report(percent);

                _logger.LogInformation("Processed {Processed}/{Total} products ({Percent}%)", processed, total, percent);
            }

            _logger.LogInformation("XML import completed successfully. Total products: {Total}", total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing XML");
            throw;
        }
    }

    // ---------------------------------------------------------
    // PROCESSOR 1: Ebijuteri (Old Format)
    // ---------------------------------------------------------
    private async Task ProcessBatchEbijuteriAsync(
        List<XElement> batch,
        HashSet<string> existingProductSet,
        Dictionary<string, int> brandDict,
        Dictionary<string, int> categoryCache)
    {
        // 1. Identify IDs (Format 1: "id")
        var batchIds = batch.Select(x => x.Element("id")?.Value)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Distinct()
                            .ToList();

        var existingProductsInBatch = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => batchIds.Contains(p.ExternalId))
            .ToDictionaryAsync(p => p.ExternalId!);

        var productsToAdd = new List<Product>();
        var productsToUpdate = new List<Product>();

        var imagesToAdd = new List<ProductImage>();
        var attributesToAdd = new List<ProductAttribute>();

        var imagesToRemoveIds = new List<int>();
        var attributesToRemoveIds = new List<int>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Pre-process categories/brands
            foreach (var item in batch)
            {
                // Brand: "marka"
                var brandName = item.Element("marka")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && !brandDict.ContainsKey(brandName))
                {
                    var newBrand = new Brand { Name = brandName, IsActive = true };
                    _context.Brands.Add(newBrand);
                    await _context.SaveChangesAsync();
                    brandDict[brandName] = newBrand.Id;
                }

                // Category: "AnaKategori"
                var categoryPath = item.Element("AnaKategori")?.Value;
                if (!string.IsNullOrEmpty(categoryPath))
                {
                    await EnsureCategoryPathExistsAsync(categoryPath, categoryCache);
                }
            }

            // Process Products
            foreach (var item in batch)
            {
                string? externalId = item.Element("id")?.Value;
                if (string.IsNullOrEmpty(externalId)) continue;

                // Stock Check
                int stock = 0;
                if (int.TryParse(item.Element("miktar")?.Value, out int s)) stock = s;
                if (stock <= 2) continue;
                
                Product product;
                bool isExisting = existingProductsInBatch.TryGetValue(externalId, out var existingProduct);

                if (isExisting)
                {
                    product = existingProduct!;
                    imagesToRemoveIds.AddRange(product.Images.Select(img => img.Id));
                    attributesToRemoveIds.AddRange(product.Attributes.Select(attr => attr.Id));
                }
                else
                {
                    product = new Product
                    {
                        ExternalId = externalId,
                        CreatedDate = DateTime.Now
                    };
                }

                // Map Fields (Ebijuteri)
                product.Name = item.Element("adi")?.Value ?? "";
                product.SKU = item.Element("stok_kodu")?.Value;
                product.Barcode = item.Element("barcode")?.Value;
                product.Description = item.Element("aciklama")?.Value;
                product.UpdatedDate = DateTime.Now;
                product.IsActive = true;
                product.Stock = stock; // Set stock here

                // Brand
                var brandName = item.Element("marka")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && brandDict.TryGetValue(brandName, out int bId))
                {
                    product.BrandId = bId;
                }

                // Category
                var categoryPath = item.Element("AnaKategori")?.Value;
                if (!string.IsNullOrEmpty(categoryPath))
                {
                    var leafId = GetLeafCategoryId(categoryPath, categoryCache);
                    if (leafId.HasValue) product.CategoryId = leafId.Value;
                }

                // Price
                var priceElement = item.Element("fiyat");
                if (priceElement != null)
                {
                    if (decimal.TryParse(priceElement.Element("son_kullanici")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                        product.Price = price;
                    if (decimal.TryParse(priceElement.Element("bayi_fiyati")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dealerPrice))
                        product.DealerPrice = dealerPrice;
                    product.Currency = priceElement.Element("para_birimi")?.Value ?? "TL";
                }

                if (int.TryParse(item.Element("kdv")?.Value, out int vat)) product.VAT = vat;

                if (!isExisting) productsToAdd.Add(product);
                else productsToUpdate.Add(product);
            }

            // Save Products
            if (productsToAdd.Any())
            {
                await _context.Products.AddRangeAsync(productsToAdd);
                await _context.SaveChangesAsync();
            }
            await _context.SaveChangesAsync(); // Update existing

            // Clean old relations
            if (imagesToRemoveIds.Any()) await _context.ProductImages.Where(x => imagesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (attributesToRemoveIds.Any()) await _context.ProductAttributes.Where(x => attributesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();

            // Add new relations
            foreach (var item in batch)
            {
                string? externalId = item.Element("id")?.Value;
                if (string.IsNullOrEmpty(externalId)) continue;

                var product = IsProductExisting(externalId) ? existingProductsInBatch[externalId] : productsToAdd.FirstOrDefault(p => p.ExternalId == externalId);
                if (product == null) continue;

                // Images: "resim" children
                var resimElement = item.Element("resim");
                if (resimElement != null)
                {
                    int sort = 1;
                    foreach (var img in resimElement.Elements())
                    {
                        if (!string.IsNullOrEmpty(img.Value))
                        {
                            imagesToAdd.Add(new ProductImage { ProductId = product.Id, ImageUrl = img.Value, IsMain = sort == 1, SortOrder = sort++ });
                        }
                    }
                }

                // Attributes: "filtreler" > "filtre"
                var filtersElement = item.Element("filtreler");
                if (filtersElement != null)
                {
                    foreach (var filter in filtersElement.Elements("filtre"))
                    {
                        var name = filter.Element("name")?.Value;
                        var value = filter.Element("value")?.Value;
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                        {
                            attributesToAdd.Add(new ProductAttribute { ProductId = product.Id, Name = name, Value = value });
                        }
                    }
                }
            }

            if (imagesToAdd.Any()) await _context.ProductImages.AddRangeAsync(imagesToAdd);
            if (attributesToAdd.Any()) await _context.ProductAttributes.AddRangeAsync(attributesToAdd);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        bool IsProductExisting(string eid) => existingProductsInBatch.ContainsKey(eid);
    }

    // ---------------------------------------------------------
    // PROCESSOR 2: XmlBankasi (New Format)
    // ---------------------------------------------------------
    private async Task ProcessBatchXmlBankasiAsync(
        List<XElement> batch,
        HashSet<string> existingProductSet,
        Dictionary<string, int> brandDict,
        Dictionary<string, int> categoryCache)
    {
        // 1. Identify IDs (Format 2: "Product_code" or "Barcode")
        var batchIds = batch.Select(x => x.Element("Product_code")?.Value?.Trim() ?? x.Element("Barcode")?.Value?.Trim())
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Distinct()
                            .ToList();

        var existingProductsInBatch = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => batchIds.Contains(p.ExternalId))
            .ToDictionaryAsync(p => p.ExternalId!);

        var productsToAdd = new List<Product>();
        var productsToUpdate = new List<Product>();

        var imagesToAdd = new List<ProductImage>();
        var attributesToAdd = new List<ProductAttribute>();

        var imagesToRemoveIds = new List<int>();
        var attributesToRemoveIds = new List<int>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Pre-process categories/brands
            foreach (var item in batch)
            {
                // Brand: "Marka"
                var brandName = item.Element("Marka")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && !brandDict.ContainsKey(brandName))
                {
                    var newBrand = new Brand { Name = brandName, IsActive = true };
                    _context.Brands.Add(newBrand);
                    await _context.SaveChangesAsync();
                    brandDict[brandName] = newBrand.Id;
                }

                // Category: "mainCategory" | "category" | "subCategory"
                var parts = new[] {
                    item.Element("mainCategory")?.Value?.Trim(),
                    item.Element("category")?.Value?.Trim(),
                    item.Element("subCategory")?.Value?.Trim()
                }.Where(x => !string.IsNullOrEmpty(x));

                if (parts.Any())
                {
                    string categoryPath = string.Join("|", parts);
                    await EnsureCategoryPathExistsAsync(categoryPath, categoryCache);
                }
            }

            // Process Products
            foreach (var item in batch)
            {
                string? externalId = item.Element("Product_code")?.Value?.Trim();
                if (string.IsNullOrEmpty(externalId)) externalId = item.Element("Barcode")?.Value?.Trim();

                if (string.IsNullOrEmpty(externalId)) continue;

                // Stock Check
                int stock = 0;
                if (int.TryParse(item.Element("Stock")?.Value, out int s)) stock = s;
                if (stock <= 2) continue;

                Product product;
                bool isExisting = existingProductsInBatch.TryGetValue(externalId, out var existingProduct);

                if (isExisting)
                {
                    product = existingProduct!;
                    imagesToRemoveIds.AddRange(product.Images.Select(img => img.Id));
                    attributesToRemoveIds.AddRange(product.Attributes.Select(attr => attr.Id));
                }
                else
                {
                    product = new Product
                    {
                        ExternalId = externalId,
                        CreatedDate = DateTime.Now
                    };
                }

                // Map Fields (XmlBankasi)
                product.Name = item.Element("Name")?.Value?.Trim() ?? "";
                product.SKU = item.Element("ModelKod")?.Value?.Trim();
                product.Barcode = item.Element("Barcode")?.Value?.Trim();
                product.Description = item.Element("Description")?.Value?.Trim();

                // Price: "xml_bayii_alis_fiyati"
                if (decimal.TryParse(item.Element("xml_bayii_alis_fiyati")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                    product.Price = price;

                product.Currency = item.Element("CurrencyType")?.Value?.Trim() ?? "TL";

                product.Stock = stock; // Set stock here
                if (int.TryParse(item.Element("Tax")?.Value, out int vat)) product.VAT = vat;

                product.UpdatedDate = DateTime.Now;
                product.IsActive = true;

                // Brand
                var brandName = item.Element("Marka")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && brandDict.TryGetValue(brandName, out int bId))
                {
                    product.BrandId = bId;
                }

                // Category
                var parts = new[] {
                    item.Element("mainCategory")?.Value?.Trim(),
                    item.Element("category")?.Value?.Trim(),
                    item.Element("subCategory")?.Value?.Trim()
                }.Where(x => !string.IsNullOrEmpty(x));

                if (parts.Any())
                {
                    string categoryPath = string.Join("|", parts);
                    var leafId = GetLeafCategoryId(categoryPath, categoryCache);
                    if (leafId.HasValue) product.CategoryId = leafId.Value;
                }

                if (!isExisting) productsToAdd.Add(product);
                else productsToUpdate.Add(product);
            }

            // Save Products
            if (productsToAdd.Any())
            {
                await _context.Products.AddRangeAsync(productsToAdd);
                await _context.SaveChangesAsync();
            }
            await _context.SaveChangesAsync();

            // Clean old relations
            if (imagesToRemoveIds.Any()) await _context.ProductImages.Where(x => imagesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (attributesToRemoveIds.Any()) await _context.ProductAttributes.Where(x => attributesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();

            // Add new relations
            foreach (var item in batch)
            {
                string? externalId = item.Element("Product_code")?.Value?.Trim() ?? item.Element("Barcode")?.Value?.Trim();
                if (string.IsNullOrEmpty(externalId)) continue;

                var product = IsProductExisting(externalId) ? existingProductsInBatch[externalId] : productsToAdd.FirstOrDefault(p => p.ExternalId == externalId);
                if (product == null) continue;

                // Images: Image1..Image5
                for (int i = 1; i <= 5; i++)
                {
                    var val = item.Element($"Image{i}")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        imagesToAdd.Add(new ProductImage { ProductId = product.Id, ImageUrl = val, SortOrder = i, IsMain = i == 1 });
                    }
                }

                // Attributes: Fixed Keys
                var attrKeys = new[] { "Renk", "Materyal", "En", "Boy", "Derinlik", "Hacim", "Desi", "ParcaSayisi" };
                foreach (var key in attrKeys)
                {
                    var val = item.Element(key)?.Value?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        attributesToAdd.Add(new ProductAttribute { ProductId = product.Id, Name = key, Value = val });
                    }
                }
            }

            if (imagesToAdd.Any()) await _context.ProductImages.AddRangeAsync(imagesToAdd);
            if (attributesToAdd.Any()) await _context.ProductAttributes.AddRangeAsync(attributesToAdd);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        bool IsProductExisting(string eid) => existingProductsInBatch.ContainsKey(eid);
    }

    // ---------------------------------------------------------
    // PROCESSOR 3: YntIthalat (Format 3)
    // ---------------------------------------------------------
    private async Task ProcessBatchYntIthalatAsync(
        List<XElement> batch,
        HashSet<string> existingProductSet,
        Dictionary<string, int> brandDict,
        Dictionary<string, int> categoryCache)
    {
        // 1. Identify IDs (Format 3: "id" or "productCode")
        // Use "id" as primary external ID as it seems unique and distinct in Ynt schema
        var batchIds = batch.Select(x => x.Element("id")?.Value?.Trim())
                            .Where(x => !string.IsNullOrEmpty(x))
                            .Distinct()
                            .ToList();

        var existingProductsInBatch = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Attributes)
            .Where(p => batchIds.Contains(p.ExternalId))
            .ToDictionaryAsync(p => p.ExternalId!);

        var productsToAdd = new List<Product>();
        var productsToUpdate = new List<Product>();

        var imagesToAdd = new List<ProductImage>();
        var attributesToAdd = new List<ProductAttribute>();

        var imagesToRemoveIds = new List<int>();
        var attributesToRemoveIds = new List<int>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Pre-process categories/brands
            foreach (var item in batch)
            {
                // Brand: "brand"
                var brandName = item.Element("brand")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && !brandDict.ContainsKey(brandName))
                {
                    var newBrand = new Brand { Name = brandName, IsActive = true };
                    _context.Brands.Add(newBrand);
                    await _context.SaveChangesAsync();
                    brandDict[brandName] = newBrand.Id;
                }

                // Category: "category" (e.g. "A >>> B >>> C")
                var catStr = item.Element("category")?.Value;
                if (!string.IsNullOrEmpty(catStr))
                {
                    string categoryPath = catStr.Replace(" >>> ", "|");
                    await EnsureCategoryPathExistsAsync(categoryPath, categoryCache);
                }
            }

            // Process Products
            foreach (var item in batch)
            {
                string? externalId = item.Element("id")?.Value?.Trim();
                if (string.IsNullOrEmpty(externalId)) continue;

                // Stock Check
                int stock = 0;
                if (int.TryParse(item.Element("quantity")?.Value, out int s)) stock = s;
                if (stock <= 2) continue;

                Product product;
                bool isExisting = existingProductsInBatch.TryGetValue(externalId, out var existingProduct);

                if (isExisting)
                {
                    product = existingProduct!;
                    imagesToRemoveIds.AddRange(product.Images.Select(img => img.Id));
                    attributesToRemoveIds.AddRange(product.Attributes.Select(attr => attr.Id));
                }
                else
                {
                    product = new Product
                    {
                        ExternalId = externalId,
                        CreatedDate = DateTime.Now
                    };
                }

                // Map Fields (YntIthalat)
                product.Name = item.Element("name")?.Value?.Trim() ?? "";
                product.SKU = item.Element("productCode")?.Value?.Trim();
                product.Barcode = item.Element("barcode")?.Value?.Trim();
                product.Description = item.Element("detail")?.Value?.Trim(); // Ynt uses "detail" (HTML)

                // Price: "price"
                if (decimal.TryParse(item.Element("price")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                    product.Price = price;

                product.Currency = item.Element("currency")?.Value?.Trim() ?? "TL";

                product.Stock = stock; // Set stock here

                // Tax: 0.2 -> 20. Need to multiply by 100? Or store as is? 
                // Product.VAT is usually integer (e.g. 18 or 20).
                // Ynt sends "0.2" or "0.18".
                if (decimal.TryParse(item.Element("tax")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal taxRate))
                    product.VAT = (int)(taxRate * 100);

                product.UpdatedDate = DateTime.Now;
                product.IsActive = item.Element("active")?.Value != "0";

                // Brand
                var brandName = item.Element("brand")?.Value?.Trim();
                if (!string.IsNullOrEmpty(brandName) && brandDict.TryGetValue(brandName, out int bId))
                {
                    product.BrandId = bId;
                }

                // Category
                var catStr = item.Element("category")?.Value;
                if (!string.IsNullOrEmpty(catStr))
                {
                    string categoryPath = catStr.Replace(" >>> ", "|");
                    var leafId = GetLeafCategoryId(categoryPath, categoryCache);
                    if (leafId.HasValue) product.CategoryId = leafId.Value;
                }

                if (!isExisting) productsToAdd.Add(product);
                else productsToUpdate.Add(product);
            }

            // Save Products
            if (productsToAdd.Any())
            {
                await _context.Products.AddRangeAsync(productsToAdd);
                await _context.SaveChangesAsync();
            }
            await _context.SaveChangesAsync();

            // Clean old relations
            if (imagesToRemoveIds.Any()) await _context.ProductImages.Where(x => imagesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();
            if (attributesToRemoveIds.Any()) await _context.ProductAttributes.Where(x => attributesToRemoveIds.Contains(x.Id)).ExecuteDeleteAsync();

            // Add new relations
            foreach (var item in batch)
            {
                string? externalId = item.Element("id")?.Value?.Trim();
                if (string.IsNullOrEmpty(externalId)) continue;

                var product = IsProductExisting(externalId) ? existingProductsInBatch[externalId] : productsToAdd.FirstOrDefault(p => p.ExternalId == externalId);
                if (product == null) continue;

                // Images: image1..image5 (lowercase i)
                for (int i = 1; i <= 5; i++)
                {
                    var val = item.Element($"image{i}")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        imagesToAdd.Add(new ProductImage { ProductId = product.Id, ImageUrl = val, SortOrder = i, IsMain = i == 1 });
                    }
                }

                // Attributes? Ynt doesn't show many separate attribute fields in snippet, mainly main fields.
                // Could parsing unit or desi?
                var unit = item.Element("unit")?.Value?.Trim();
                if (!string.IsNullOrEmpty(unit)) attributesToAdd.Add(new ProductAttribute { ProductId = product.Id, Name = "Birim", Value = unit });

                var desi = item.Element("desi")?.Value?.Trim();
                if (!string.IsNullOrEmpty(desi)) attributesToAdd.Add(new ProductAttribute { ProductId = product.Id, Name = "Desi", Value = desi });
            }

            if (imagesToAdd.Any()) await _context.ProductImages.AddRangeAsync(imagesToAdd);
            if (attributesToAdd.Any()) await _context.ProductAttributes.AddRangeAsync(attributesToAdd);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        bool IsProductExisting(string eid) => existingProductsInBatch.ContainsKey(eid);
    }

    private async Task EnsureCategoryPathExistsAsync(string categoryPath, Dictionary<string, int> categoryCache)
    {
        var parts = categoryPath.Split('|', StringSplitOptions.RemoveEmptyEntries);
        int parentId = 0;

        foreach (var part in parts)
        {
            var name = part.Trim();
            if (string.IsNullOrEmpty(name)) continue;

            string key = $"{parentId}|{name}";
            if (categoryCache.TryGetValue(key, out int existingId))
            {
                parentId = existingId;
            }
            else
            {
                // Create new
                var newCat = new Category
                {
                    Name = name,
                    ParentId = parentId == 0 ? null : parentId,
                    IsActive = true
                };
                _context.Categories.Add(newCat);
                await _context.SaveChangesAsync(); // Need ID immediately

                parentId = newCat.Id;
                categoryCache[key] = parentId;
            }
        }
    }

    private int? GetLeafCategoryId(string categoryPath, Dictionary<string, int> categoryCache)
    {
        var parts = categoryPath.Split('|', StringSplitOptions.RemoveEmptyEntries);
        int parentId = 0;
        int? lastId = null;

        foreach (var part in parts)
        {
            var name = part.Trim();
            string key = $"{parentId}|{name}";
            if (categoryCache.TryGetValue(key, out int id))
            {
                parentId = id;
                lastId = id;
            }
            else
            {
                return null; // Should not happen if Ensured called before
            }
        }
        return lastId;
    }
}
