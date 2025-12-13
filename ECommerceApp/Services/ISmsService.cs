namespace ECommerceApp.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string message, List<string> phoneNumbers);
    }
}
