using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class SeoSetting
    {
        public int Id { get; set; }

        [Display(Name = "Sayfa Adı")]
        [Required(ErrorMessage = "Sayfa adı zorunludur (Örn: Anasayfa, İletişim)")]
        public string PageName { get; set; }

        [Display(Name = "URL Yolu")]
        [Required(ErrorMessage = "URL yolu zorunludur (Örn: / veya /iletisim)")]
        public string UrlPath { get; set; }

        [Display(Name = "Meta Başlık (Title)")]
        [Required]
        [MaxLength(160)]
        public string Title { get; set; }

        [Display(Name = "Meta Açıklama (Description)")]
        [MaxLength(300)]
        public string Description { get; set; }

        [Display(Name = "Anahtar Kelimeler (Keywords)")]
        [MaxLength(200)]
        public string Keywords { get; set; }

        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}

