using ECommerceApp.Models;

namespace ECommerceApp.Services;

public interface INavlungoService
{
    Task<NavlungoShipmentResult> CreateShipmentAsync(Order order);
    Task<string> GetShippingLabelAsync(string postNumber); // Returns PDF URL or Base64
}
