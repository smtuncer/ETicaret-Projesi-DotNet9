using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Ad gereklidir")]
        [StringLength(50, ErrorMessage = "Ad en fazla 50 karakter olabilir")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Soyad gereklidir")]
        [StringLength(50, ErrorMessage = "Soyad en fazla 50 karakter olabilir")]
        public string? Surname { get; set; }

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Şifre gereklidir")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
        public string? ConfirmPassword { get; set; }
    }
}
