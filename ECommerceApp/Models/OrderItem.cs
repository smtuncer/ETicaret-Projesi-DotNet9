using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class OrderItem
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; }

    public int? ProductId { get; set; }
    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; }

    public string ProductName { get; set; } // Ürün silinirse diye saklıyoruz
    public decimal Price { get; set; }      // Satış anındaki fiyat
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }
}
