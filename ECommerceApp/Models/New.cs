using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceApp.Models;

public class New
{
    [Key]
    public int Id { get; set; }
    public string? Image { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? MetaTagKeywords { get; set; }
    public string? MetaTagDescription { get; set; }
    public string? MetaTagTitle { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime UpdatedTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsDuyuru { get; set; }
    [NotMapped]
    public int? FullTextLength { get; set; }
}
