namespace ECommerceApp.Services
{
    public interface ISeoService
    {
        Task<ECommerceApp.Models.SeoSetting?> GetSeoSettingAsync(string urlPath);
        void ClearCache();
    }
}

