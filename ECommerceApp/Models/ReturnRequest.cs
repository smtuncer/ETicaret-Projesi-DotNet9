using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public enum ReturnRequestStatus
{
    [Display(Name = "Bekliyor")]
    Pending = 0,
    [Display(Name = "Onaylandı")]
    Approved = 1,
    [Display(Name = "Reddedildi")]
    Rejected = 2,
    [Display(Name = "İptal Edildi")] // Kullanıcı kendisi iptal ederse
    Cancelled = 3
}

public class ReturnRequest
{
    [Key]
    public int Id { get; set; }

    public int OrderId { get; set; }
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; }

    public int OrderItemId { get; set; }
    [ForeignKey("OrderItemId")]
    public virtual OrderItem OrderItem { get; set; }

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    [Required]
    [Display(Name = "İade Nedeni")]
    public string Reason { get; set; }

    [Display(Name = "Durum")]
    public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.Pending;

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Admin Notu")]
    public string? AdminNote { get; set; }
}
