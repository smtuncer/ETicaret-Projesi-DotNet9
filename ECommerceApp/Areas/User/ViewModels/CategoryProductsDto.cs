using ECommerceApp.Models.DTOs.Product;

namespace ECommerceApp.Areas.User.ViewModels;

public class CategoryProductsDto
{
    public string CategoryName { get; set; }
    public string CategoryUrl { get; set; }
    public List<ProductSummaryDto> Products { get; set; } = new List<ProductSummaryDto>();
}
