using ECommerceApp.Models;

namespace ECommerceApp.Services;

public interface IPayTrService
{
    Task<string> GetIframeTokenAsync(Order order, User user, List<OrderItem> orderItems, string userIp);
    bool ValidateCallback(string merchantOid, string status, string totalAmount, string receivedHash);
}
