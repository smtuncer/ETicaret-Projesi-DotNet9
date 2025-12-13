using ECommerceApp.Models.DTOs.Product;

namespace ECommerceApp.Areas.User.ViewModels
{
    public class FeaturedProductsViewModel
    {
        public List<ProductSummaryDto> Products { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}

