using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class ProductComment
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [Required(ErrorMessage = "Ad Soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string Name { get; set; }

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(150)]
    [Display(Name = "E-posta")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Yorum zorunludur.")]
    [StringLength(1000, ErrorMessage = "Yorum en fazla 1000 karakter olabilir.")]
    [Display(Name = "Yorum")]
    public string Comment { get; set; }

    [Display(Name = "Puan")]
    [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalıdır.")]
    public int Rating { get; set; } = 5;

    [Display(Name = "Onaylı mı?")]
    public bool IsApproved { get; set; } = false;

    [Display(Name = "Yönetici Cevabı")]
    public string? AdminReply { get; set; }

    [Display(Name = "Yazılma Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public string? IpAddress { get; set; }
}
