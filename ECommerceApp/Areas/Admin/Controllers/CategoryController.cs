using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toast;

    public CategoryController(ApplicationDbContext context, IToastNotification toast)
    {
        _context = context;
        _toast = toast;
    }

    public async Task<IActionResult> Index(string search, int page = 1)
    {
        var allCategories = await _context.Categories
            .Include(c => c.Parent)
            .AsNoTracking()
            .ToListAsync();

        List<Category> sortedCategories;
        var categoryDepths = new Dictionary<int, int>();

        if (!string.IsNullOrEmpty(search))
        {
            sortedCategories = allCategories
                .Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.ParentId != null)
                .ThenBy(c => c.Order)
                .ThenBy(c => c.Name)
                .ToList();
        }
        else
        {
            sortedCategories = new List<Category>();
            // Hierarchy sort
            void AddCategory(Category cat, int level)
            {
                sortedCategories.Add(cat);
                categoryDepths[cat.Id] = level;

                var children = allCategories
                    .Where(c => c.ParentId == cat.Id)
                    .OrderBy(c => c.Order)
                    .ThenBy(c => c.Name);

                foreach (var child in children)
                {
                    AddCategory(child, level + 1);
                }
            }

            var roots = allCategories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name);

            foreach (var root in roots)
            {
                AddCategory(root, 0);
            }
        }

        int pageSize = 25;
        int totalItems = sortedCategories.Count;
        int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var pagedCategories = sortedCategories
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Get Product Counts for visible categories only
        var catIds = pagedCategories.Select(c => c.Id).ToList();
        var counts = new Dictionary<int, int>();
        if (catIds.Any())
        {
            var rawCounts = await _context.Products
                .Where(p => p.CategoryId.HasValue && catIds.Contains(p.CategoryId.Value))
                .GroupBy(p => p.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var rc in rawCounts)
            {
                if (rc.CategoryId.HasValue) counts[rc.CategoryId.Value] = rc.Count;
            }
        }

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalItems = totalItems;
        ViewBag.CategoryDepths = categoryDepths;
        ViewBag.ProductCounts = counts;

        // Populate Parent Categories for Dropdowns (Recursive)
        ViewBag.ParentCategories = GetCategorySelectList(allCategories);

        return View(pagedCategories);
    }

    private List<SelectListItem> GetCategorySelectList(List<Category> allCategories)
    {
        var list = new List<SelectListItem>();

        void AddCategoryToSelect(Category cat, string prefix)
        {
            list.Add(new SelectListItem { Value = cat.Id.ToString(), Text = prefix + cat.Name });
            var children = allCategories
                .Where(c => c.ParentId == cat.Id)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name);

            foreach (var child in children)
            {
                AddCategoryToSelect(child, prefix + "-- ");
            }
        }

        var roots = allCategories
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Name);

        foreach (var root in roots)
        {
            AddCategoryToSelect(root, "");
        }

        return list;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        if (ModelState.IsValid)
        {
            _context.Add(category);
            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage("Kategori başarıyla eklendi.", new ToastrOptions { Title = "Başarılı" });
            return RedirectToAction(nameof(Index));
        }
        _toast.AddErrorToastMessage("Kategori eklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category category)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Kategori güncellendi.", new ToastrOptions { Title = "Başarılı" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(category.Id))
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
        _toast.AddErrorToastMessage("Güncelleme başarısız.", new ToastrOptions { Title = "Hata" });
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            // Check if has subcategories or products
            var hasDependencies = await _context.Categories.AnyAsync(c => c.ParentId == id) ||
                                  await _context.Products.AnyAsync(p => p.CategoryId == id);

            if (hasDependencies)
            {
                _toast.AddErrorToastMessage("Bu kategoriye bağlı alt kategoriler veya ürünler var. Önce onları silmelisiniz.", new ToastrOptions { Title = "Hata" });
            }
            else
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage("Kategori silindi.", new ToastrOptions { Title = "Başarılı" });
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSelected(List<int> ids)
    {
        if (ids == null || !ids.Any())
        {
            _toast.AddErrorToastMessage("Lütfen silinecek kategorileri seçiniz.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Silinecek tüm kategori ID'lerini (alt kategoriler dahil) topla
            var allCategoryIdsToDelete = new HashSet<int>();
            foreach (var id in ids)
            {
                await CollectCategoryIdsRecursive(id, allCategoryIdsToDelete);
            }

            var categoryIdsList = allCategoryIdsToDelete.ToList();

            if (categoryIdsList.Any())
            {
                // 2. Bu kategorilerdeki ürün ID'lerini bul
                var productIds = await _context.Products
                    .Where(p => p.CategoryId.HasValue && categoryIdsList.Contains(p.CategoryId.Value))
                    .Select(p => p.Id)
                    .ToListAsync();

                if (productIds.Any())
                {
                    // 3. Toplu Silme İşlemleri (Batch Delete/Update)
                    // ExecuteDeleteAsync ve ExecuteUpdateAsync veritabanında doğrudan çalışır ve çok daha hızlıdır.

                    // 3.1. Sepet Öğeleri
                    await _context.CartItems
                        .Where(ci => productIds.Contains(ci.ProductId))
                        .ExecuteDeleteAsync();

                    // 3.2. Ürün Özellikleri
                    await _context.ProductAttributes
                        .Where(pa => productIds.Contains(pa.ProductId))
                        .ExecuteDeleteAsync();

                    // 3.3. Ürün Resimleri
                    await _context.ProductImages
                        .Where(pi => productIds.Contains(pi.ProductId))
                        .ExecuteDeleteAsync();

                    // 3.4. Sipariş Detayları (ProductId = null yap)
                    await _context.OrderItems
                        .Where(oi => oi.ProductId.HasValue && productIds.Contains(oi.ProductId.Value))
                        .ExecuteUpdateAsync(s => s.SetProperty(oi => oi.ProductId, (int?)null));

                    // 3.5. Ürünleri Sil
                    await _context.Products
                        .Where(p => p.CategoryId.HasValue && categoryIdsList.Contains(p.CategoryId.Value))
                        .ExecuteDeleteAsync();
                }

                // 4. Kategorileri Sil
                await _context.Categories
                    .Where(c => categoryIdsList.Contains(c.Id))
                    .ExecuteDeleteAsync();
            }

            await transaction.CommitAsync();
            _toast.AddSuccessToastMessage($"{categoryIdsList.Count} kategori ve ilişkili ürünler başarıyla silindi.", new ToastrOptions { Title = "Başarılı" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _toast.AddErrorToastMessage("Silme işlemi sırasında bir hata oluştu: " + ex.Message, new ToastrOptions { Title = "Hata" });
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task CollectCategoryIdsRecursive(int categoryId, HashSet<int> collectedIds)
    {
        if (!collectedIds.Add(categoryId)) return; // Zaten eklendiyse çık

        var subCategoryIds = await _context.Categories
            .Where(c => c.ParentId == categoryId)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var subId in subCategoryIds)
        {
            await CollectCategoryIdsRecursive(subId, collectedIds);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel()
    {
        var categories = await _context.Categories
            .Include(c => c.Parent)
            .OrderBy(c => c.ParentId)
            .ThenBy(c => c.Order)
            .ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Kategoriler");

            // Headers
            worksheet.Cell(1, 1).Value = "Id";
            worksheet.Cell(1, 2).Value = "Ad";
            worksheet.Cell(1, 3).Value = "Aciklama";
            worksheet.Cell(1, 4).Value = "UstKategori";
            worksheet.Cell(1, 5).Value = "Sira";
            worksheet.Cell(1, 6).Value = "AktifMi";
            worksheet.Cell(1, 7).Value = "MenudeGoster";
            worksheet.Cell(1, 8).Value = "OneCikar";

            // Data
            for (int i = 0; i < categories.Count; i++)
            {
                var row = i + 2;
                var item = categories[i];

                worksheet.Cell(row, 1).Value = item.Id;
                worksheet.Cell(row, 2).Value = item.Name;
                worksheet.Cell(row, 3).Value = item.Description?.Length > 32000 ? item.Description.Substring(0, 32000) : item.Description;
                worksheet.Cell(row, 4).Value = item.Parent?.Name;
                worksheet.Cell(row, 5).Value = item.Order;
                worksheet.Cell(row, 6).Value = item.IsActive ? "Evet" : "Hayır";
                worksheet.Cell(row, 7).Value = item.ShowInMenu ? "Evet" : "Hayır";
                worksheet.Cell(row, 8).Value = item.IsFeatured ? "Evet" : "Hayır";
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"kategoriler_{DateTime.Now:yyyyMMddHHmm}.xlsx");
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
                    var allCategories = await _context.Categories.ToListAsync();
                    var catDict = allCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase); // Name -> Id

                    int addedCount = 0;
                    int updatedCount = 0;

                    foreach (var row in rows)
                    {
                        var idVal = row.Cell(1).GetValue<string>();
                        int.TryParse(idVal, out int id);

                        Category category;
                        bool isNew = false;

                        if (id > 0)
                        {
                            category = await _context.Categories.FindAsync(id);
                            if (category == null)
                            {
                                // ID provided but not found, create new
                                category = new Category();
                                isNew = true;
                            }
                        }
                        else
                        {
                            category = new Category();
                            isNew = true;
                        }

                        // Basic Fields
                        category.Name = row.Cell(2).GetValue<string>();
                        category.Description = row.Cell(3).GetValue<string>();

                        // Parse Parent
                        var parentName = row.Cell(4).GetValue<string>()?.Trim();
                        if (!string.IsNullOrEmpty(parentName))
                        {
                            if (catDict.TryGetValue(parentName, out int parentId))
                            {
                                // Avoid circular reference if update
                                if (!isNew && parentId == category.Id)
                                {
                                    category.ParentId = null;
                                }
                                else
                                {
                                    category.ParentId = parentId;
                                }
                            }
                            else
                            {
                                // Try to find by ID if user put ID in name column? Or just null.
                                // Let's keep it null if not found. Or maybe create? Creating parents on fly is complex in single pass.
                                category.ParentId = null;
                            }
                        }
                        else
                        {
                            category.ParentId = null;
                        }

                        var orderVal = row.Cell(5).GetValue<string>();
                        if (int.TryParse(orderVal, out int order)) category.Order = order;

                        var activeVal = row.Cell(6).GetValue<string>();
                        category.IsActive = activeVal?.Equals("Evet", StringComparison.OrdinalIgnoreCase) == true;

                        var menuVal = row.Cell(7).GetValue<string>();
                        category.ShowInMenu = menuVal?.Equals("Evet", StringComparison.OrdinalIgnoreCase) == true;

                        var featVal = row.Cell(8).GetValue<string>();
                        category.IsFeatured = featVal?.Equals("Evet", StringComparison.OrdinalIgnoreCase) == true;

                        if (isNew)
                        {
                            _context.Categories.Add(category);
                            // We need to save to get ID for next rows if they rely on this? 
                            // Bulk save is faster but prevents finding newly added parents in same batch by dict.
                            // Let's save individually if we want robust parent lookup, OR save at end.
                            // Given expected volume is low for categories (<100s), saving individually is fine.
                            await _context.SaveChangesAsync();
                            catDict[category.Name] = category.Id; // Add to cache
                            addedCount++;
                        }
                        else
                        {
                            _context.Update(category);
                            await _context.SaveChangesAsync();
                            // Update cache name just in case
                            catDict[category.Name] = category.Id;
                            updatedCount++;
                        }
                    }

                    _toast.AddSuccessToastMessage($"İçe aktarım tamamlandı. {addedCount} yeni, {updatedCount} güncellenen kategori.", new ToastrOptions { Title = "Başarılı" });
                }
            }
        }
        catch (Exception ex)
        {
            _toast.AddErrorToastMessage("Hata oluştu: " + ex.Message, new ToastrOptions { Title = "Hata" });
        }

        return RedirectToAction(nameof(Index));
    }

    private bool CategoryExists(int id)
    {
        return _context.Categories.Any(e => e.Id == id);
    }
}
