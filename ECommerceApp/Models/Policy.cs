using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class Policy
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Başlık alanı zorunludur.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "İçerik alanı zorunludur.")]
        public string Content { get; set; }

        public string Slug { get; set; }

        public bool IsActive { get; set; } = true;

        public int Order { get; set; }
    }
}
