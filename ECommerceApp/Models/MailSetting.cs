using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class MailSetting
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Gönderen adı zorunludur")]
    [Display(Name = "Gönderen Adı")]
    public string SenderName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gönderen e-posta adresi zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [Display(Name = "Gönderen E-posta")]
    public string SenderEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sunucu adresi zorunludur")]
    [Display(Name = "SMTP Sunucusu")]
    public string Server { get; set; } = string.Empty;

    [Required(ErrorMessage = "Port numarası zorunludur")]
    [Display(Name = "Port")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
    [Display(Name = "Kullanıcı Adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur")]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "SSL Kullan")]
    public bool EnableSsl { get; set; } = true;
}
