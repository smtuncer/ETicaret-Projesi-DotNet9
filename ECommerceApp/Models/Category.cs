using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur")]
    [Display(Name = "Kategori Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Üst Kategori")]
    public int? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public virtual Category? Parent { get; set; }

    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Sıra")]
    public int Order { get; set; }

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Anasayfada Göster")]
    public bool IsFeatured { get; set; } = false;

    [Display(Name = "Menüde Göster")]
    public bool ShowInMenu { get; set; } = false;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
