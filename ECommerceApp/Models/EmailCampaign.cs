using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models;

public class EmailCampaign
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kampanya başlığı zorunludur")]
    [Display(Name = "Başlık")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik zorunludur")]
    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Hedef Kitle")]
    public CampaignTargetAudience TargetAudience { get; set; }

    [Display(Name = "Oluşturulma Tarihi")]
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Display(Name = "Gönderilme Tarihi")]
    public DateTime? SentDate { get; set; }

    [Display(Name = "Durum")]
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
}

public enum CampaignTargetAudience
{
    [Display(Name = "Tüm Kullanıcılar")]
    AllUsers,

    [Display(Name = "Sadece Aboneler")]
    Subscribers
}

public enum CampaignStatus
{
    [Display(Name = "Taslak")]
    Draft,

    [Display(Name = "Gönderildi")]
    Sent,

    [Display(Name = "Gönderiliyor")]
    Sending,

    [Display(Name = "Hata")]
    Error
}
