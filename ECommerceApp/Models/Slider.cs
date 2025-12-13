using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class Slider
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur")]
    [Display(Name = "Başlık")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Alt Başlık")]
    [StringLength(500)]
    public string? Subtitle { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Görsel URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Mobil Görsel URL")]
    public string? MobileImageUrl { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [Display(Name = "Masaüstü Görseli")]
    public IFormFile? ImageUpload { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    [Display(Name = "Mobil Görseli")]
    public IFormFile? MobileImageUpload { get; set; }

    [Display(Name = "Link URL")]
    public string? LinkUrl { get; set; }

    [Display(Name = "Buton Metni")]
    [StringLength(50)]
    public string? ButtonText { get; set; }

    [Display(Name = "Sıra")]
    public int Order { get; set; } = 0;

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Oluşturma Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
