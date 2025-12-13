using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models
{
    public class Blog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [Required]
        [StringLength(300)]
        public string? Slug { get; set; }

        [Required]
        public string? Content { get; set; }

        [StringLength(500)]
        public string? Summary { get; set; }

        [StringLength(500)]
        public string? FeaturedImage { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual BlogCategory? Category { get; set; }

        public int? AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public virtual User? Author { get; set; }

        public int ViewCount { get; set; } = 0;

        public bool IsPublished { get; set; } = false;

        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? PublishedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        [StringLength(200)]
        public string? MetaKeywords { get; set; }

        public virtual ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
    }
}
