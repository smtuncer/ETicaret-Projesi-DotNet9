using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class ProductAttribute
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [Required]
    [Display(Name = "Özellik Adı")]
    public string Name { get; set; } = string.Empty; // XML: filtre name

    [Required]
    [Display(Name = "Değer")]
    public string Value { get; set; } = string.Empty; // XML: filtre value
}
