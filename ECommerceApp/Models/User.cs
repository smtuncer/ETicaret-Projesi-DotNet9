using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Surname { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(11)]
        [Display(Name = "TC Kimlik No")]
        public string? IdentityNumber { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "User";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string? PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpires { get; set; }

        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        public virtual ICollection<PaymentNotification> PaymentNotifications { get; set; } = new List<PaymentNotification>();

        public virtual ICollection<ProductComment> ProductComments { get; set; } = new List<ProductComment>();
    }
}
