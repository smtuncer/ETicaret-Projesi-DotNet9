using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.ViewComponents
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public HeaderViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Fetch all active categories from DB without tracking (faster)
            var allCategories = await _context.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Order)
                .ThenBy(c => c.Name)
                .ToListAsync();

            // 2. Create a lookup for efficiently finding categories by Id
            // We use a Dictionary for O(1) access
            var categoryLookup = allCategories.ToDictionary(c => c.Id);

            // 3. Prepare a list to hold root categories
            var rootCategories = new List<Category>();

            // 4. Iterate over all categories to build the tree
            foreach (var category in allCategories)
            {
                // Ensure SubCategories is initialized and empty (since we are creating the tree manually)
                // Note: Since we used AsNoTracking, these are plain objects, we can modify them freely.
                category.SubCategories = new List<Category>();
            }

            foreach (var category in allCategories)
            {


                if (category.ParentId.HasValue && categoryLookup.TryGetValue(category.ParentId.Value, out var parent))
                {
                    // If it has a parent, add it to the parent's SubCategories
                    parent.SubCategories.Add(category);
                }
                else
                {
                    // If no parent (or parent not active/found), it's a root category
                    rootCategories.Add(category);
                }
            }

            // 5. Return the root categories which now contain the full tree of children
            return View(rootCategories);
        }
    }
}
