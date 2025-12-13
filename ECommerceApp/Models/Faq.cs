using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class Faq
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Soru alanı zorunludur.")]
        public string Question { get; set; }

        [Required(ErrorMessage = "Cevap alanı zorunludur.")]
        public string Answer { get; set; }

        [Required(ErrorMessage = "Kategori alanı zorunludur.")]
        public string Category { get; set; }

        public int Order { get; set; }
    }
}
