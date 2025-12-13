using ECommerceApp.Models.DTOs.Contact;

namespace ECommerceApp.Areas.User.ViewModels;

public class ContactPageViewModel
{
    public ContactInfoDto ContactInfo { get; set; } = new();
    public ContactFormDto Form { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string? SuccessMessage { get; set; }
}

