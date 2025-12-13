using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public enum OrderStatus
{
    Pending = 0,        // Beklemede (Ödeme Bekleniyor)
    Approved = 1,       // Onaylandı (Ödeme Alındı / Hazırlanıyor)
    Shipped = 2,        // Kargolandı
    Delivered = 3,      // Teslim Edildi
    Cancelled = 4,      // İptal Edildi
    Refunded = 5        // İade Edildi
}

public enum PaymentMethod
{
    CreditCard = 0,
    BankTransfer = 1,
    CreditCardIyzico = 2
}

public class Order
{
    [Key]
    public int Id { get; set; }

    public string OrderNumber { get; set; } // Sipariş No (örn: ORD-20231025-123)

    public int UserId { get; set; }
    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentMethod PaymentMethod { get; set; }

    public decimal TotalAmount { get; set; }

    // İndirim Kodu Bilgileri (Kupon uygulandıysa)
    [Display(Name = "İndirim Kodu")]
    public string? CouponCode { get; set; }

    [Display(Name = "İndirim Tutarı")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    [Display(Name = "İndirim Öncesi Tutar")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? SubTotalAmount { get; set; }

    // KDV ve Kargo Bilgileri
    [Display(Name = "KDV Oranı (%)")]
    public decimal VatRate { get; set; } = 20;

    [Display(Name = "KDV Tutarı")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }

    [Display(Name = "Kargo Ücreti")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingFee { get; set; }

    [Display(Name = "Ara Toplam (KDV Hariç)")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotalWithoutVat { get; set; }

    // Shipping Address Snapshot (Adres silinse bile siparişte kalmalı)
    public string ShippingAddressTitle { get; set; }
    public string ShippingAddressCity { get; set; }
    public string ShippingAddressDistrict { get; set; }
    public string ShippingAddressDetail { get; set; }
    public string ShippingZipCode { get; set; }

    // Billing Address Snapshot
    [Display(Name = "Fatura Adresi Başlığı")]
    public string BillingAddressTitle { get; set; }

    [Display(Name = "Fatura İli")]
    public string BillingAddressCity { get; set; }

    [Display(Name = "Fatura İlçesi")]
    public string BillingAddressDistrict { get; set; }

    [Display(Name = "Fatura Açık Adres")]
    public string BillingAddressDetail { get; set; }

    [Display(Name = "Fatura Posta Kodu")]
    public string? BillingZipCode { get; set; }

    // Corporate / Individual Billing Details
    [Display(Name = "TC Kimlik No")]
    public string? BillingIdentityNumber { get; set; }

    [Display(Name = "Şirket Adı")]
    public string? BillingCompanyName { get; set; }

    [Display(Name = "Vergi Dairesi")]
    public string? BillingTaxOffice { get; set; }

    [Display(Name = "Vergi No")]
    public string? BillingTaxNumber { get; set; }

    // Kargo Bilgileri
    [Display(Name = "Kargo Notu")]
    public string? ShippingNote { get; set; }

    [Display(Name = "Kargo Notu Tarihi")]
    public DateTime? ShippingNoteDate { get; set; }

    [Display(Name = "Kargo Notu Ekleyen")]
    public string? ShippingNoteBy { get; set; }

    // Navlungo Integration
    public string? NavlungoPostNumber { get; set; }
    public string? NavlungoBarcodeUrl { get; set; }

    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public virtual ICollection<PaymentNotification> PaymentNotifications { get; set; } = new List<PaymentNotification>();
}
