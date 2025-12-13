using ECommerceApp.Models;

namespace ECommerceApp.Areas.Admin.Models.ViewModels
{
    public class UserDetailsVM
    {
        public ECommerceApp.Models.User User { get; set; }
        public List<Order> Orders { get; set; }
        public List<PaymentNotification> PaymentNotifications { get; set; }
        public List<ProductComment> ProductComments { get; set; }
    }
}
