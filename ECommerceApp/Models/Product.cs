using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur")]
    [Display(Name = "Ürün Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "SEO URL (Slug)")]
    [StringLength(200)]
    public string? Slug { get; set; }

    [Display(Name = "Stok Kodu (SKU)")]
    public string? SKU { get; set; }

    [Display(Name = "Harici ID")]
    public string? ExternalId { get; set; } // XML'den gelen id/product_id

    [Display(Name = "Barkod")]
    public string? Barcode { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Satış Fiyatı")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Display(Name = "Bayi Fiyatı")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DealerPrice { get; set; }

    [Display(Name = "Para Birimi")]
    public string Currency { get; set; } = "TL";

    [Display(Name = "KDV Oranı")]
    public int VAT { get; set; } = 20;

    [Display(Name = "Stok Miktarı")]
    public int Stock { get; set; } = 0;

    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }

    [Display(Name = "Marka")]
    public int? BrandId { get; set; }

    [ForeignKey("BrandId")]
    public virtual Brand? Brand { get; set; }

    [Display(Name = "Eklenme Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Güncellenme Tarihi")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Öne Çıkarıldı mı?")]
    public bool IsFeatured { get; set; } = false;

    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    public virtual ICollection<ProductComment> Comments { get; set; } = new List<ProductComment>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
