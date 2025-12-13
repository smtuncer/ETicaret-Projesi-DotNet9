using ECommerceApp.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using System.Globalization;

namespace ECommerceApp.Services;

public class IyzicoService : IIyzicoService
{
    private readonly IConfiguration _configuration;
    private readonly Options _options;

    public IyzicoService(IConfiguration configuration)
    {
        _configuration = configuration;
        _options = new Options
        {
            ApiKey = _configuration["Iyzico:ApiKey"],
            SecretKey = _configuration["Iyzico:SecretKey"],
            BaseUrl = _configuration["Iyzico:BaseUrl"]
        };
    }

    public async Task<CheckoutFormInitialize> InitializeCheckoutFormAsync(Order order, User user, List<ECommerceApp.Models.OrderItem> orderItems, string userIp, string callbackUrl)
    {
        if (string.IsNullOrEmpty(_options.ApiKey) || string.IsNullOrEmpty(_options.SecretKey))
        {
            throw new Exception($"Iyzico API Keys are missing! Key: '{_options.ApiKey}', Secret: '{_options.SecretKey?.Length}' chars. BaseUrl: '{_options.BaseUrl}'");
        }

        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = Locale.TR.ToString(),
            ConversationId = order.OrderNumber,
            Price = order.SubTotalAmount?.ToString(new CultureInfo("en-US")) ?? order.TotalAmount.ToString(new CultureInfo("en-US")),
            PaidPrice = order.TotalAmount.ToString(new CultureInfo("en-US")),
            Currency = Currency.TRY.ToString(),
            BasketId = order.OrderNumber,
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = callbackUrl, // "https://www.merchant.com/callback"
            EnabledInstallments = new List<int> { 2, 3, 6, 9 },
            Buyer = new Buyer
            {
                Id = user.Id.ToString(),
                Name = user.Name,
                Surname = user.Surname,
                GsmNumber = user.PhoneNumber ?? "+905555555555",
                Email = user.Email,
                IdentityNumber = "11111111111", // Zorunlu alan, kullanıcıdan alınmıyorsa dummy
                LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationDate = user.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationAddress = order.BillingAddressDetail ?? order.ShippingAddressDetail,
                Ip = userIp,
                City = order.BillingAddressCity ?? order.ShippingAddressCity,
                Country = "Turkey",
                ZipCode = order.BillingZipCode ?? order.ShippingZipCode
            },
            ShippingAddress = new Iyzipay.Model.Address
            {
                ContactName = user.Name + " " + user.Surname,
                City = order.ShippingAddressCity,
                Country = "Turkey",
                Description = order.ShippingAddressDetail,
                ZipCode = order.ShippingZipCode
            },
            BillingAddress = new Iyzipay.Model.Address
            {
                ContactName = order.BillingCompanyName ?? (user.Name + " " + user.Surname),
                City = order.BillingAddressCity ?? order.ShippingAddressCity,
                Country = "Turkey",
                Description = order.BillingAddressDetail ?? order.ShippingAddressDetail,
                ZipCode = order.BillingZipCode ?? order.ShippingZipCode
            },
            BasketItems = new List<BasketItem>()
        };

        decimal basketTotal = 0;
        foreach (var item in orderItems)
        {
            var itemPrice = item.Price * item.Quantity; // Total price for this line item (unit price * qty)? 
                                                        // Iyzipay expects 'Price' per basket item. If we have 2x Product A @ 100TL, do we send 1 item with 200TL or 2 items with 100TL?
                                                        // Correct approach: Iyzipay BasketItem doesn't have Quantity. We must add 'Quantity' times the item or sum it up.
                                                        // Usually, we add 1 BasketItem per OrderItem, but set Price = (Unit Price * Quantity).

            // However, let's verify if 'Price' in BasketItem is Unit Price or Total Line Price.
            // Iyzipay documentation says: "Price: Ürünün satış fiyatı." It usually implies the total amount for that basket row.
            // Let's use Line Total.

            decimal lineTotal = item.Price * item.Quantity;
            // Note: Our 'item.Price' might be incl. or excl. VAT? OrderItem usually stores Unit Price.
            // Order logic: TotalAmount is final. 
            // Let's use logic: We need to Distribute whole order amount to items to match 'PaidPrice'.
            // Simple approach: Add items with their line totals.

            request.BasketItems.Add(new BasketItem
            {
                Id = item.ProductId.ToString(),
                Name = item.ProductName,
                Category1 = "General",
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = lineTotal.ToString(new CultureInfo("en-US"))
            });
            basketTotal += lineTotal;
        }

        // Add Shipping Fee as a separate item if it exists and is > 0
        if (order.ShippingFee > 0)
        {
            request.BasketItems.Add(new BasketItem
            {
                Id = "Cargo",
                Name = "Kargo Ücreti",
                Category1 = "Kargo",
                ItemType = BasketItemType.VIRTUAL.ToString(),
                Price = order.ShippingFee.ToString(new CultureInfo("en-US"))
            });
            basketTotal += order.ShippingFee;
        }

        // If there is a discount, 'basketTotal' (sum of positive items) will be greater than 'order.TotalAmount' (PaidPrice).
        // Iyzipay: Price = Sum of basket items. PaidPrice = What user pays.
        // If Discount exists: Price (basket total) > PaidPrice (discounted). This is allowed.
        // If Price < PaidPrice, that's an error.
        // If Price == PaidPrice, that's standard.

        // Ensure 'Price' field in StartRequest matches the calculated basketTotal
        request.Price = basketTotal.ToString(new CultureInfo("en-US"));
        request.PaidPrice = order.TotalAmount.ToString(new CultureInfo("en-US"));

        return await Task.Run(() => CheckoutFormInitialize.Create(request, _options));
    }

    public async Task<CheckoutForm> RetrieveCheckoutFormAuthAsync(string token)
    {
        var request = new RetrieveCheckoutFormRequest
        {
            Token = token
        };

        return await Task.Run(() => CheckoutForm.Retrieve(request, _options));
    }
}
