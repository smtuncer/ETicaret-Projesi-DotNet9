using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models.DTOs.Contact;

public class ContactFormDto
{
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
}

