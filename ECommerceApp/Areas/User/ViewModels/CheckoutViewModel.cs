using ECommerceApp.Models;

namespace ECommerceApp.Areas.User.ViewModels;

public class CheckoutViewModel
{
    public Cart Cart { get; set; }
    public List<Address> UserAddresses { get; set; } = new List<Address>();
    public int SelectedAddressId { get; set; }

    // For new address
    public Address NewAddress { get; set; } = new Address();
    public bool UseNewAddress { get; set; }

    public bool UseDifferentBillingAddress { get; set; } = false;
    public bool UseNewBillingAddress { get; set; }
    public int SelectedBillingAddressId { get; set; }
    public Address NewBillingAddress { get; set; } = new Address();

    // Payment info (placeholder for now)
    public string PaymentMethod { get; set; } = "CreditCard";
}
