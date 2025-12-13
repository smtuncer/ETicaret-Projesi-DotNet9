using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class Brand
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Marka adı zorunludur")]
    [Display(Name = "Marka Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Logo URL")]
    public string? LogoUrl { get; set; }

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
