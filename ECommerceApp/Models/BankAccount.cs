using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class BankAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string BankName { get; set; }

    [Required]
    [StringLength(100)]
    public string AccountHolder { get; set; }

    [Required]
    [StringLength(50)]
    public string IBAN { get; set; }

    [StringLength(50)]
    public string AccountNumber { get; set; }

    [StringLength(50)]
    public string BranchCode { get; set; }

    public bool IsActive { get; set; } = true;
}
