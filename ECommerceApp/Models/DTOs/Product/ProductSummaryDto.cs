namespace ECommerceApp.Models.DTOs.Product;

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string? Slug { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public string? BrandName { get; set; }
    public string Currency { get; set; } = "₺";
}

