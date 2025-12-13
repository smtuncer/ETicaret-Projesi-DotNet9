using ECommerceApp.Models;
using ECommerceApp.Models.DTOs.Product;

namespace ECommerceApp.Areas.User.ViewModels;

public class ProductDetailViewModel
{
    public ProductDetailDto Product { get; set; } = new();
    public IList<string> ImageUrls { get; set; } = new List<string>();
    public IList<ProductAttributeDto> Attributes { get; set; } = new List<ProductAttributeDto>();
    public IList<ProductSummaryDto> RelatedProducts { get; set; } = new List<ProductSummaryDto>();
    public IList<ProductSummaryDto> FeaturedProducts { get; set; } = new List<ProductSummaryDto>();
    public IList<ProductComment> Comments { get; set; } = new List<ProductComment>();
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public bool UserHasPurchased { get; set; }
    public bool IsFavored { get; set; }
}

