using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceApp.Services
{
    public class SeoService : ISeoService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "all_seo_settings";

        public SeoService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<SeoSetting?> GetSeoSettingAsync(string urlPath)
        {
            if (string.IsNullOrEmpty(urlPath)) return null;

            var allSettings = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                return await _context.SeoSettings.AsNoTracking().ToListAsync();
            });

            // Normalleştirme: Başta/sonda slash varsa temizleyip karşılaştırabiliriz veya exact match yapabiliriz.
            // Şimdilik exact match ve case-insensitive yapalım.
            var setting = allSettings?.FirstOrDefault(x =>
                x.UrlPath.Equals(urlPath, StringComparison.OrdinalIgnoreCase) ||
                x.UrlPath.Trim('/').Equals(urlPath.Trim('/'), StringComparison.OrdinalIgnoreCase)
            );

            return setting;
        }

        public void ClearCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}

