using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models
{
    public class BlogCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [StringLength(150)]
        public string? Slug { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
