using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class ContactMessage
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    [Display(Name = "Ad Soyad")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu zorunludur")]
    [Display(Name = "Konu")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj zorunludur")]
    [Display(Name = "Mesaj")]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Gönderilme Tarihi")]
    public DateTime SentDate { get; set; } = DateTime.Now;

    [Display(Name = "Okundu")]
    public bool IsRead { get; set; } = false;

    [Display(Name = "Cevaplandı")]
    public bool IsReplied { get; set; } = false;
}
