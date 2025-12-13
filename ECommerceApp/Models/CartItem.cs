using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class CartItem
{
    [Key]
    public int Id { get; set; }

    public int CartId { get; set; }

    [ForeignKey("CartId")]
    public virtual Cart Cart { get; set; }

    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    public int Quantity { get; set; } = 1;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [NotMapped]
    public decimal VatRate { get; set; }

    [NotMapped]
    public decimal VatAmount { get; set; }

    [NotMapped]
    public decimal PriceWithVat { get; set; }
}
