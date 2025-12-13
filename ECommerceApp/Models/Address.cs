using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required(ErrorMessage = "Adres başlığı zorunludur")]
        [StringLength(50)]
        [Display(Name = "Adres Başlığı")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Telefon zorunludur")]
        [Phone]
        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Ülke zorunludur")]
        [StringLength(50)]
        [Display(Name = "Ülke")]
        public string? Country { get; set; } = "Türkiye";

        [Required(ErrorMessage = "Şehir zorunludur")]
        [StringLength(50)]
        [Display(Name = "Şehir")]
        public string? City { get; set; }

        [Required(ErrorMessage = "İlçe zorunludur")]
        [StringLength(50)]
        [Display(Name = "İlçe")]
        public string? District { get; set; }

        [Required(ErrorMessage = "Açık adres zorunludur")]
        [StringLength(500)]
        [Display(Name = "Açık Adres")]
        public string? OpenAddress { get; set; }

        [StringLength(10)]
        [Display(Name = "Posta Kodu")]
        public string? ZipCode { get; set; }

        [Display(Name = "Fatura Adresi mi?")]
        public bool IsBillingAddress { get; set; } = false;

        // Fatura bilgileri (sadece IsBillingAddress = true ise gerekli)
        [StringLength(11)]
        [Display(Name = "TC Kimlik No")]
        public string? IdentityNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Şirket Adı")]
        public string? CompanyName { get; set; }

        [StringLength(100)]
        [Display(Name = "Vergi Dairesi")]
        public string? TaxOffice { get; set; }

        [StringLength(20)]
        [Display(Name = "Vergi No")]
        public string? TaxNumber { get; set; }

        [StringLength(500)]
        [Display(Name = "Fatura Adresi")]
        public string? BillingAddress { get; set; }

        [Display(Name = "Oluşturma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
