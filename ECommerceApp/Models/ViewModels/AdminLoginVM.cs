using System.ComponentModel.DataAnnotations;

namespace ECommerceApp.Models.ViewModels;

public class AdminLoginVM
{
    [Required]
    public string Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}
