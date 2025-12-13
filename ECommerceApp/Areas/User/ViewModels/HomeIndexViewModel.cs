using ECommerceApp.Models.DTOs.Product;

namespace ECommerceApp.Areas.User.ViewModels;

public class HomeIndexViewModel
{
    public IList<ECommerceApp.Models.Slider> Sliders { get; set; } = new List<ECommerceApp.Models.Slider>();
    public IList<ProductSummaryDto> FeaturedProducts { get; set; } = new List<ProductSummaryDto>();
    public IList<ProductSummaryDto> BestSellerProducts { get; set; } = new List<ProductSummaryDto>();
    public IList<ECommerceApp.Models.Blog> LatestBlogs { get; set; } = new List<ECommerceApp.Models.Blog>();
    public IList<CategoryProductsDto> FeaturedCategoryProducts { get; set; } = new List<CategoryProductsDto>();

}


