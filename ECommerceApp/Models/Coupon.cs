using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public enum DiscountType
{
    Percentage = 1,
    Amount = 2
}

public class Coupon
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kupon kodu zorunludur.")]
    [StringLength(50)]
    public string Code { get; set; }

    [Display(Name = "İndirim Tipi")]
    public DiscountType DiscountType { get; set; }

    [Display(Name = "İndirim Değeri")]
    public decimal DiscountValue { get; set; }

    [Display(Name = "Minimum Sepet Tutarı")]
    public decimal? MinCartAmount { get; set; }

    [Display(Name = "Başlangıç Tarihi")]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Display(Name = "Bitiş Tarihi")]
    public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

    [Display(Name = "Aktif mi?")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
