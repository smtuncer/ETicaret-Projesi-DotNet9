using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.ViewComponents
{
    public class SeoTagsViewComponent : ViewComponent
    {
        private readonly ISeoService _seoService;

        public SeoTagsViewComponent(ISeoService seoService)
        {
            _seoService = seoService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Öncelik: Controller tarafından set edilmiş ViewData varsa onu kullan
            // Bu sayede ürün detay gibi dinamik sayfalarda controller'dan gelen veri ezilmez.
            var viewDataTitle = ViewData["Title"] as string;
            var viewDataDesc = ViewData["MetaDescription"] as string;
            var viewDataKeywords = ViewData["MetaKeywords"] as string;

            // 2. Eğer ViewData boşsa veya genel bir sayfa ise veritabanına bak
            var path = Request.Path.Value ?? "/";
            var seoSetting = await _seoService.GetSeoSettingAsync(path);

            var model = new SeoSetting
            {
                Title = !string.IsNullOrEmpty(viewDataTitle) ? viewDataTitle : (seoSetting?.Title ?? "Luxda - E-Ticaret"),
                Description = !string.IsNullOrEmpty(viewDataDesc) ? viewDataDesc : (seoSetting?.Description ?? "Luxda online alışveriş."),
                Keywords = !string.IsNullOrEmpty(viewDataKeywords) ? viewDataKeywords : (seoSetting?.Keywords ?? "e-ticaret, alışveriş, luxda"),
                UrlPath = path
            };

            return View(model);
        }
    }
}

