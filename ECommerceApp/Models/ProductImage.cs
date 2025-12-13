using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [Required]
    [Display(Name = "Resim URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Display(Name = "Ana Resim mi?")]
    public bool IsMain { get; set; } = false;

    [Display(Name = "Sıra")]
    public int SortOrder { get; set; } = 0;
}
