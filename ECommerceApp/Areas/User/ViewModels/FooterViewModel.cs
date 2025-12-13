using ECommerceApp.Models;
using ECommerceApp.Models.DTOs.Contact;

namespace ECommerceApp.Areas.User.ViewModels;

public class FooterViewModel
{
    public IList<Category> Categories { get; set; } = new List<Category>();
    public IList<Policy> Policies { get; set; } = new List<Policy>();
    public ContactInfoDto ContactInfo { get; set; } = new();
}
