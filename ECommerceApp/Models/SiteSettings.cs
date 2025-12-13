using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class SiteSettings
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "KDV Oranı (%)")]
    [Range(0, 100)]
    public decimal VatRate { get; set; } = 20; // Varsayılan %20

    [Display(Name = "Kargo Ücreti (₺)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingFee { get; set; } = 0; // Varsayılan ücretsiz

    [Display(Name = "Ücretsiz Kargo Minimum Tutarı (₺)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingThreshold { get; set; } = 500; // 500 TL üzeri ücretsiz

    [Display(Name = "İletişim E-Posta")]
    [EmailAddress]
    public string ContactEmail { get; set; } = "info@example.com";

    [Display(Name = "Güncellenme Tarihi")]
    public DateTime UpdatedDate { get; set; } = DateTime.Now;
}
