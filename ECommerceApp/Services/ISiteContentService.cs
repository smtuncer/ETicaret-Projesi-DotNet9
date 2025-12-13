using ECommerceApp.Models;

namespace ECommerceApp.Services;

public interface ISiteContentService
{
    Task<SiteContent> GetSiteContentAsync();
    Task CreateSiteContentAsync(SiteContent model);
    Task UpdateSiteContentAsync(SiteContent model);
}