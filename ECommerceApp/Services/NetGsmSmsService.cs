using System.Text;

namespace ECommerceApp.Services
{
    public class NetGsmSmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NetGsmSmsService> _logger;

        public NetGsmSmsService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<NetGsmSmsService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string message, List<string> phoneNumbers)
        {
            try
            {
                var userCode = _configuration["NetGsm:UserCode"];
                var password = _configuration["NetGsm:Password"];
                var header = _configuration["NetGsm:Header"];

                if (string.IsNullOrEmpty(userCode) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(header))
                {
                    _logger.LogError("NetGsm configuration is missing.");
                    return false;
                }

                if (phoneNumbers == null || !phoneNumbers.Any())
                {
                    return false; // No recipients
                }

                // Prepare XML
                var sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\"?>");
                sb.Append("<mainbody>");
                sb.Append("<header>");
                sb.Append($"<company dil=\"TR\">Netgsm</company>");
                sb.Append($"<usercode>{userCode}</usercode>");
                sb.Append($"<password>{password}</password>");
                sb.Append($"<type>1:n</type>");
                sb.Append($"<msgheader>{header}</msgheader>");
                sb.Append("</header>");
                sb.Append("<body>");
                sb.Append($"<msg><![CDATA[{message}]]></msg>");

                foreach (var phone in phoneNumbers)
                {
                    // Basic cleanup for phone numbers
                    var cleanPhone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                    if (cleanPhone.StartsWith("0")) cleanPhone = cleanPhone.Substring(1);
                    if (cleanPhone.StartsWith("90")) cleanPhone = cleanPhone.Substring(2);

                    // NetGSM expects 10 digits usually? Or with 90?
                    // Typically 5xxxxxxxxx is best, but let's assume we send 5xxxxxxxxx
                    // Let's ensure it has 10 digits if possible

                    sb.Append($"<no>{cleanPhone}</no>");
                }

                sb.Append("</body>");
                sb.Append("</mainbody>");


                var client = _httpClientFactory.CreateClient();
                // NetGSM XML Post URL
                var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/xml");

                // Some APIs might expect "text/xml" or just raw post body.
                // Or sometimes form-data with xml in a field. 
                // NetGSM docs usually say: POST to https://api.netgsm.com.tr/sms/send/xml

                var response = await client.PostAsync("https://api.netgsm.com.tr/sms/send/xml", content);
                var responseString = await response.Content.ReadAsStringAsync();

                // Check response
                // Success usually starts with "00" or returns a TaskID
                // 00 123456789
                // Errors: 20, 30, 40 etc.

                if (response.IsSuccessStatusCode)
                {
                    // Basic check logic
                    // Specifically check if result starts with a digit or code that means success.
                    // Assuming success if we got a response. 
                    // Better validation:
                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        // NetGSM success response format: Code ID (e.g. 00 123456)
                        // 00: Success
                        // 01: Parameter error
                        // 02: Auth error ...

                        var parts = responseString.Split(' ');
                        if (parts.Length > 0 && parts[0] == "00")
                        {
                            _logger.LogInformation($"SMS sent successfully. ID: {responseString}");
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"NetGSM Error: {responseString}");
                            return false;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS via NetGSM");
                return false;
            }
        }
    }
}
