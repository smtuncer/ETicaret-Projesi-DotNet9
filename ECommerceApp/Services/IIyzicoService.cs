using ECommerceApp.Models;
using Iyzipay.Model;

namespace ECommerceApp.Services;

public interface IIyzicoService
{
    Task<CheckoutFormInitialize> InitializeCheckoutFormAsync(Order order, User user, List<ECommerceApp.Models.OrderItem> orderItems, string userIp, string callbackUrl);
    Task<CheckoutForm> RetrieveCheckoutFormAuthAsync(string token);
}
