using ECommerceApp.Models;
using ECommerceApp.Models.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ECommerceApp.Services;

public class SiteContentService : ISiteContentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private const string CacheKey = "SiteContent";

    public SiteContentService(ApplicationDbContext context, IMemoryCache cache, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _cache = cache;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<SiteContent> GetSiteContentAsync()
    {
        if (!_cache.TryGetValue(CacheKey, out SiteContent siteContent))
        {
            siteContent = await _context.SiteContents.OrderByDescending(s => s.Id).FirstOrDefaultAsync();

            if (siteContent != null)
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)); // Önbellek süresini ihtiyaca göre ayarlayın

                _ = _cache.Set(CacheKey, siteContent, cacheEntryOptions);
            }
        }

        return siteContent;
    }

    public async Task CreateSiteContentAsync(SiteContent model)
    {

        _ = await _context.SiteContents.AddAsync(model);
        _ = await _context.SaveChangesAsync();

        // Önbelleği güncelle ve ilgili tüm cache'leri temizle
        _ = _cache.Set(CacheKey, model, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(10)
        });

        // FooterVC ve NavbarVC cache'lerini temizle
        ClearRelatedCaches();
    }

    public async Task UpdateSiteContentAsync(SiteContent model)
    {
        var existingContent = await _context.SiteContents.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (existingContent == null)
        {
            throw new KeyNotFoundException("Site içeriği bulunamadı.");
        }


        UpdateSiteContentProperties(existingContent, model);

        _ = _context.SiteContents.Update(existingContent);
        _ = await _context.SaveChangesAsync();

        // Önbelleği güncelle ve ilgili tüm cache'leri temizle
        _ = _cache.Set(CacheKey, existingContent, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(10)
        });

        // FooterVC ve NavbarVC cache'lerini temizle
        ClearRelatedCaches();
    }

    private void UpdateSiteContentProperties(SiteContent existingContent, SiteContent updatedContent)
    {
        existingContent.CorporateSalesContent = updatedContent.CorporateSalesContent;
        existingContent.CorporateSalesTitle = updatedContent.CorporateSalesTitle;
        existingContent.ContactAddress = updatedContent.ContactAddress;
        existingContent.ContactPhoneNumber = updatedContent.ContactPhoneNumber;
        existingContent.ContactText = updatedContent.ContactText;
        existingContent.ContactEmail = updatedContent.ContactEmail;
        existingContent.ContactGoogleMap = updatedContent.ContactGoogleMap;
        existingContent.ContactOpeningHour = updatedContent.ContactOpeningHour;
        existingContent.WhatsappButtonPhoneNumber = updatedContent.WhatsappButtonPhoneNumber;
        existingContent.WhatsappButtonText = updatedContent.WhatsappButtonText;
        existingContent.FacebookURL = updatedContent.FacebookURL;
        existingContent.InstagramURL = updatedContent.InstagramURL;
        existingContent.TwitterURL = updatedContent.TwitterURL;
        existingContent.YoutubeURL = updatedContent.YoutubeURL;
        existingContent.LinkedinURL = updatedContent.LinkedinURL;
        // Resim alanları zaten dosya yükleme işlemlerinde güncellendi
    }


    /// <summary>
    /// SiteContent değiştiğinde ilgili tüm ViewComponent cache'lerini temizler
    /// </summary>
    private void ClearRelatedCaches()
    {
        // ViewComponent cache key'leri
        _cache.Remove("footer_cache_key");                  // FooterVC
        _cache.Remove("navbar_cache_key");                  // NavbarVC

        _cache.Remove("contact_page_cache");                // İletişim Page
    }
}
