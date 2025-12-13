namespace ECommerceApp.Models.DTOs.Product;

public class ProductDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "₺";
    public string? SKU { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }
    public int Stock { get; set; }
    public string? Slug { get; set; }
    public DateTime CreatedDate { get; set; }
}

