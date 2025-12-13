using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class Cart
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();

    public int? CouponId { get; set; }

    [ForeignKey("CouponId")]
    public virtual Coupon? Coupon { get; set; }

    [NotMapped]
    public decimal SubTotal { get; set; }

    [NotMapped]
    public decimal DiscountAmount { get; set; }

    [NotMapped]
    public decimal TotalVAT { get; set; }

    [NotMapped]
    public decimal ShippingCost { get; set; } = 0;

    [NotMapped]
    public decimal TotalAmount { get; set; }
}
