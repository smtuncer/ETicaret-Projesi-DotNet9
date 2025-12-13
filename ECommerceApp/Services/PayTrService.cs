using ECommerceApp.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ECommerceApp.Services;

public class PayTrService : IPayTrService
{
    private readonly IConfiguration _configuration;

    public PayTrService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GetIframeTokenAsync(Order order, User user, List<OrderItem> orderItems, string userIp)
    {
        string merchantId = _configuration["PayTR:MerchantId"];
        string merchantKey = _configuration["PayTR:MerchantKey"];
        string merchantSalt = _configuration["PayTR:MerchantSalt"];
        string testModeStr = _configuration["PayTR:TestMode"] ?? "1";

        string email = user.Email;
        int paymentAmount = (int)(order.TotalAmount * 100);
        string merchantOid = order.OrderNumber;
        string userName = user.Name + " " + user.Surname;
        string userAddress = order.ShippingAddressDetail + " " + order.ShippingAddressDistrict + " " + order.ShippingAddressCity;
        string userPhone = user.PhoneNumber ?? "05555555555";

        // Use current request scheme and host if possible, but here we rely on config or hardcode for now.
        // Better to pass it or use IHttpContextAccessor.
        // For now I'll use a placeholder or config.
        string domain = _configuration["PayTR:Domain"] ?? "localhost:5000";
        string merchantOkUrl = $"https://{domain}/odeme/basarili";
        string merchantFailUrl = $"https://{domain}/odeme/basarisiz";

        // Basket
        var basketItems = orderItems.Select(x => new object[] { x.ProductName, x.Price.ToString("F2").Replace(",", "."), x.Quantity }).ToArray();
        string userBasketJson = JsonSerializer.Serialize(basketItems);

        string noInstallment = "0";
        string maxInstallment = "0";
        string currency = "TL";

        // Hash
        string concat = string.Concat(
            merchantId,
            userIp,
            merchantOid,
            email,
            paymentAmount.ToString(),
            userBasketJson,
            noInstallment,
            maxInstallment,
            currency,
            testModeStr
        );

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concat + merchantSalt));
        string paytrToken = Convert.ToBase64String(hashBytes);

        using var client = new HttpClient();
        var values = new Dictionary<string, string>
        {
            { "merchant_id", merchantId },
            { "user_ip", userIp },
            { "merchant_oid", merchantOid },
            { "email", email },
            { "payment_amount", paymentAmount.ToString() },
            { "paytr_token", paytrToken },
            { "user_basket", userBasketJson },
            { "debug_on", "1" },
            { "no_installment", noInstallment },
            { "max_installment", maxInstallment },
            { "user_name", userName },
            { "user_address", userAddress },
            { "user_phone", userPhone },
            { "merchant_ok_url", merchantOkUrl },
            { "merchant_fail_url", merchantFailUrl },
            { "timeout_limit", "30" },
            { "currency", currency },
            { "test_mode", testModeStr }
        };

        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync("https://www.paytr.com/odeme/api/get-token", content);
        var responseString = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseString);
        var root = doc.RootElement;

        if (root.GetProperty("status").GetString() == "success")
        {
            return root.GetProperty("token").GetString();
        }
        else
        {
            string reason = root.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "Unknown error";
            throw new Exception("PayTR Error: " + reason);
        }
    }

    public bool ValidateCallback(string merchantOid, string status, string totalAmount, string receivedHash)
    {
        string merchantKey = _configuration["PayTR:MerchantKey"];
        string merchantSalt = _configuration["PayTR:MerchantSalt"];

        string concat = merchantOid + status + totalAmount;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(concat + merchantSalt));
        string calculatedHash = Convert.ToBase64String(hashBytes);

        return calculatedHash == receivedHash;
    }
}
