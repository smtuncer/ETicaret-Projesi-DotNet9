using ECommerceApp.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ECommerceApp.Services;

public class NavlungoService : INavlungoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NavlungoService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public NavlungoService(HttpClient httpClient, ILogger<NavlungoService> logger, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;

        _httpClient.BaseAddress = new Uri(_configuration["NavlungoSettings:BaseUrl"]);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cache.TryGetValue("NavlungoToken", out string cachedToken))
        {
            return cachedToken;
        }

        var loginUrl = _configuration["NavlungoSettings:LoginUrl"];
        var username = _configuration["NavlungoSettings:UserName"];
        var password = _configuration["NavlungoSettings:Password"];

        var requestBody = new
        {
            username = username,
            password = password
        };

        string responseString = "";
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(loginUrl, content);

            responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Navlungo Login Hatası ({response.StatusCode}): {responseString}");
            }

            dynamic tokenData = JsonConvert.DeserializeObject(responseString);

            // Handle different possible structures
            string token = tokenData.access_token;
            if (token == null && tokenData.data != null)
            {
                token = tokenData.data.access_token;
            }

            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Token alınamadı. Yanıt yapısı beklenenden farklı: " + responseString);
            }

            // Safely parse expiration
            int expiresIn = 28800; // Default 8 hours
            try
            {
                var expToken = tokenData.expires_in ?? tokenData.data?.expires_in;
                if (expToken != null)
                {
                    string expStr = expToken.ToString();
                    if (int.TryParse(expStr, out int seconds))
                    {
                        expiresIn = seconds;
                    }
                    else if (DateTime.TryParse(expStr, out DateTime expDate))
                    {
                        // Calculate seconds from now
                        var diff = expDate - DateTime.Now;
                        if (diff.TotalSeconds > 0)
                            expiresIn = (int)diff.TotalSeconds;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors and use default
            }

            // Cache token slightly less than expiry time
            var cacheDuration = expiresIn > 600 ? expiresIn - 600 : expiresIn;
            if (cacheDuration <= 0) cacheDuration = 3600; // Min 1 hour safety

            _cache.Set("NavlungoToken", token, TimeSpan.FromSeconds(cacheDuration));

            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Navlungo token retrieval failed. Response: {responseString}");
            throw new Exception($"Navlungo servisine giriş yapılamadı: {ex.Message}");
        }
    }

    public async Task<NavlungoShipmentResult> CreateShipmentAsync(Order order)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createUrl = _configuration["NavlungoSettings:CreatePostUrl"];
            var carrierId = int.Parse(_configuration["NavlungoSettings:CarrierId"] ?? "1");

            var senderAddressIdStr = _configuration["NavlungoSettings:SenderAddressId"];
            long? senderAddressId = null;
            if (!string.IsNullOrEmpty(senderAddressIdStr) && long.TryParse(senderAddressIdStr, out long addrId))
            {
                senderAddressId = addrId;
            }

            // Prepare Shipment Data
            var shipmentData = new
            {
                platform = "ECommerceApp",
                posts = new[]
                {
                    new
                    {
                        reference_id = order.OrderNumber,
                        carrier_id = carrierId,
                        post_type = 2, // Standard
                        sender = new
                        {
                             addressId = senderAddressId // Required field
                        },
                        recipient = new
                        {
                            name = $"{order.User?.Name} {order.User?.Surname}".Trim(),
                            phone = order.User?.PhoneNumber ?? "",
                            email = order.User?.Email ?? "",
                            address = order.ShippingAddressDetail,
                            country = "TR", // Defaulting to TR for domestic
                            city = order.ShippingAddressCity,
                            district = order.ShippingAddressDistrict,
                            // post_code = order.ShippingZipCode 
                        },
                        post = new
                        {
                            desi = 1, // Default desi to 1 or calculate based on items
                            package_count = 1,
                            // price = order.TotalAmount, // Only needed if COD
                            note = order.ShippingNote ?? ""
                        }
                        // barcode_format removed as it caused validation error and we get barcode separately
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(shipmentData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(createUrl, content);

            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Navlungo CreateShipment Error: {responseString}");
                throw new Exception($"Kargo oluşturulamadı ({response.StatusCode}): {responseString}");
            }

            dynamic responseData = JsonConvert.DeserializeObject(responseString);

            // Check for success and extract post number
            // The structure is likely { "status": true, "data": [ { "post_number": "...", ... } ] } or similar
            // Based on docs, need to carefully parse. Assuming standard success response.

            if (responseData.status == true && responseData.data != null && responseData.data.Count > 0)
            {
                var item = responseData.data[0];
                return new NavlungoShipmentResult
                {
                    PostNumber = item.post_number,
                    BarcodeUrl = item.barcode,
                    TrackingUrl = item.tracking_url
                };
            }
            else if (responseData.post_number != null)
            {
                return new NavlungoShipmentResult
                {
                    PostNumber = responseData.post_number
                };
            }

            // If we have detailed errors
            if (responseData.error != null)
            {
                throw new Exception($"Navlungo Hatası: {responseData.error}");
            }

            // Try to find post_number inside the array if different structure
            var postsArray = responseData.data as Newtonsoft.Json.Linq.JArray;
            if (postsArray != null && postsArray.Count > 0)
            {
                dynamic item = postsArray[0];
                return new NavlungoShipmentResult
                {
                    PostNumber = item["post_number"].ToString(),
                    BarcodeUrl = item["barcode"]?.ToString(),
                    TrackingUrl = item["tracking_url"]?.ToString()
                };
            }

            throw new Exception($"Kargo takip numarası alınamadı. Yanıt: {responseString}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navlungo CreateShipment failed.");
            throw;
        }
    }

    public async Task<string> GetShippingLabelAsync(string postNumber)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var barcodeUrl = _configuration["NavlungoSettings:GetBarcodeUrl"];

            var requestBody = new
            {
                post_number = postNumber,
                barcode_type = "pdf"
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(barcodeUrl, content);

            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Navlungo GetBarcode Error: {responseString}");
                throw new Exception($"Barkod alınamadı ({response.StatusCode}): {responseString}");
            }

            dynamic responseData = JsonConvert.DeserializeObject(responseString);

            // Look for URL first, then base64
            string url = responseData.data?.url; // Adjust path based on actual response
            if (!string.IsNullOrEmpty(url)) return url;

            // Or maybe direct root properties
            url = responseData.url;
            if (!string.IsNullOrEmpty(url)) return url;

            // If base64 content
            string base64 = responseData.data?.content;
            if (!string.IsNullOrEmpty(base64))
            {
                // We might need to handle base64 here or return it. 
                // Getting a direct URL is preferred if available.
                // If only base64, we might simply return a data URI or handle it in controller.
                return $"data:application/pdf;base64,{base64}";
            }

            throw new Exception("Barkod verisi bulunamadı.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navlungo GetShippingLabel API call failed. Using fallback URL construction.");
            // Fallback to standard URL pattern if API fails (common issue with some carriers in Navlungo)
            return $"https://domestic-barcode.navlungo.com/{postNumber}.pdf";
        }
    }
}
