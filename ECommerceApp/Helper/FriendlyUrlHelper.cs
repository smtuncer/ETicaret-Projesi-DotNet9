using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace ECommerceApp.Helper;

public static class FriendlyUrlHelper
{
    public static string FriendlyURLTitle(this IUrlHelper urlHelper, string incomingText)
    {

        if (incomingText != null)
        {
            incomingText = incomingText.Replace("ş", "s");
            incomingText = incomingText.Replace("Ş", "s");
            incomingText = incomingText.Replace("İ", "i");
            incomingText = incomingText.Replace("I", "i");
            incomingText = incomingText.Replace("ı", "i");
            incomingText = incomingText.Replace("ö", "o");
            incomingText = incomingText.Replace("Ö", "o");
            incomingText = incomingText.Replace("ü", "u");
            incomingText = incomingText.Replace("Ü", "u");
            incomingText = incomingText.Replace("Ç", "c");
            incomingText = incomingText.Replace("ç", "c");
            incomingText = incomingText.Replace("ğ", "g");
            incomingText = incomingText.Replace("Ğ", "g");
            incomingText = incomingText.Replace(" ", "-");
            incomingText = incomingText.Replace("---", "-");
            incomingText = incomingText.Replace("?", "");
            incomingText = incomingText.Replace("/", "");
            incomingText = incomingText.Replace(".", "");
            incomingText = incomingText.Replace("'", "");
            incomingText = incomingText.Replace("#", "");
            incomingText = incomingText.Replace("%", "");
            incomingText = incomingText.Replace("&", "");
            incomingText = incomingText.Replace("*", "");
            incomingText = incomingText.Replace("!", "");
            incomingText = incomingText.Replace("@", "");
            incomingText = incomingText.Replace("+", "");
            incomingText = incomingText.ToLower();
            incomingText = incomingText.Trim();
            // tüm harfleri küçült
            string encodedUrl = (incomingText ?? "").ToLower();
            // & ile " " yer değiştirme
            encodedUrl = Regex.Replace(encodedUrl, @"\&+", "and");
            // " " karakterlerini silme
            encodedUrl = encodedUrl.Replace("'", "");
            // geçersiz karakterleri sil
            encodedUrl = Regex.Replace(encodedUrl, @"[^a-z0-9]", "-");
            // tekrar edenleri sil
            encodedUrl = Regex.Replace(encodedUrl, @"-+", "-");
            // karakterlerin arasına tire koy
            encodedUrl = encodedUrl.Trim('-');
            return encodedUrl;
        }
        else
        {
            return "";
        }
    }

    /// <summary>
    /// Generates a SEO-friendly URL for a product detail page
    /// </summary>
    public static string ProductUrl(this IUrlHelper urlHelper, int productId, string productName, string slug = null)
    {
        // If slug is provided, use it; otherwise generate from product name
        var urlSlug = !string.IsNullOrEmpty(slug) ? slug : urlHelper.FriendlyURLTitle(productName);

        return urlHelper.Action("Detail", "Product", new { slug = urlSlug, id = productId });
    }

    /// <summary>
    /// Generates a SEO-friendly URL for a blog detail page
    /// </summary>
    public static string BlogUrl(this IUrlHelper urlHelper, int blogId, string slug)
    {
        return urlHelper.Action("Detail", "Blog", new { slug = slug, id = blogId });
    }

    /// <summary>
    /// Generates a SEO-friendly URL for a category page
    /// </summary>
    public static string CategoryUrl(this IUrlHelper urlHelper, int categoryId, string categoryName)
    {
        var slug = urlHelper.FriendlyURLTitle(categoryName);
        // We will route this to Product/Index but with a cleaner URL if possible, or query param
        // The user specifically requested: /urunler?categoryId=_XXX_ -> categoryId yerine kategori adı yazmalı
        // But the example /urunler?categoryId=224 contradicts "categoryId yerine kategori adı yazmalı"
        // Wait, request says: "categoryId yerine kategori adı yazmalı ... /urunler?categoryId=224" -> "categoryId=224" is what they HAVE or WANT to AVOID?
        // "Kategori seçilirse kullandığım bu url Türkçe SEO uyumlu olmalıdır ... /urunler?categoryId=224"
        // I think they mean: Currently it is /urunler?categoryId=224. They WANT it to be SEO friendly like /urunler/elektronik-224 or /kategori/elektronik-224.
        // OR they want the query param to check name? No, usually route.
        // The prompt says: "categoryId yerine kategori adı yazmalı ... @[Helper/FriendlyUrlHelper.cs] class'ı kullanabilirsin"

        // Let's implement a route like /kategori/{slug}-{id} mapping to Product/Index with categoryId
        return urlHelper.Action("Index", "Product", new { categorySlug = slug, categoryId = categoryId });
    }
}