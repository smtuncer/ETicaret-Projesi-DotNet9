using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models
{
    public class BlogComment
    {
        [Key]
        public int Id { get; set; }

        public int BlogId { get; set; }

        [ForeignKey("BlogId")]
        public virtual Blog? Blog { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(1000)]
        public string? Comment { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string? IpAddress { get; set; }
    }
}
