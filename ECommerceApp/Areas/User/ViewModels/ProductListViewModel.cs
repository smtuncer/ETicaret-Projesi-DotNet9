using ECommerceApp.Models;

namespace ECommerceApp.Areas.User.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Brand> Brands { get; set; } = new List<Brand>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? SelectedCategoryId { get; set; }
        public string SelectedCategoryName { get; set; }
        public List<int> SelectedBrandIds { get; set; } = new List<int>();
        public string SearchTerm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortOrder { get; set; }
        public int TotalCount { get; set; }
    }
}
