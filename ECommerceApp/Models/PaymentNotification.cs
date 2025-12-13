using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public enum PaymentNotificationStatus
{
    [Display(Name = "Beklemede")]
    Pending = 0,

    [Display(Name = "Onaylandı")]
    Approved = 1,

    [Display(Name = "Reddedildi")]
    Rejected = 2
}

public class PaymentNotification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    public int? OrderId { get; set; }

    [ForeignKey("OrderId")]
    public virtual Order? Order { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Gönderen Ad Soyad")]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Gönderilen Tutar")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    [Display(Name = "Açıklama / Not")]
    public string? Description { get; set; }

    [Display(Name = "Dekont/Makbuz")]
    public string? ReceiptImageUrl { get; set; }

    [Required]
    [Display(Name = "Bildirim Tarihi")]
    public DateTime NotificationDate { get; set; } = DateTime.Now;

    [Display(Name = "İşlem Tarihi")]
    public DateTime? TransactionDate { get; set; }

    [Display(Name = "Onay Durumu")]
    public PaymentNotificationStatus Status { get; set; } = PaymentNotificationStatus.Pending;

    [StringLength(500)]
    [Display(Name = "Admin Notu")]
    public string? AdminNote { get; set; }

    [Display(Name = "Onaylayan Admin")]
    public int? ApprovedByAdminId { get; set; }

    [Display(Name = "Onay Tarihi")]
    public DateTime? ApprovedDate { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Eski property'ler için backward compatibility
    [NotMapped]
    public string? Note
    {
        get => Description;
        set => Description = value;
    }
}
