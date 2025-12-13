using ECommerceApp.Models;
using System.Globalization;
using System.Text;

namespace ECommerceApp.Services;

public static class EmailTemplates
{
    private static readonly string _style = @"
        <style>
            body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f9f9f9; }
            .container { max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
            .header { background-color: #000000; padding: 20px 40px; text-align: center; }
            .header h1 { color: #C5A059; margin: 0; font-size: 24px; text-transform: uppercase; letter-spacing: 2px; }
            .content { padding: 40px; }
            .button { display: inline-block; padding: 12px 24px; background-color: #C5A059; color: #ffffff !important; text-decoration: none; border-radius: 4px; font-weight: bold; margin-top: 20px; }
            .footer { background-color: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666; }
            .table { width: 100%; border-collapse: collapse; margin: 20px 0; }
            .table th { text-align: left; padding: 12px; border-bottom: 2px solid #eee; color: #666; font-size: 12px; text-transform: uppercase; }
            .table td { padding: 12px; border-bottom: 1px solid #eee; }
            .total-row { font-weight: bold; font-size: 16px; }
            .highlight { color: #C5A059; }
        </style>";

    public static string GetWelcomeEmailBody(string userName)
    {
        return $@"
            <html>
            <head>{_style}</head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Luxda.net</h1>
                    </div>
                    <div class='content'>
                        <h2>Aramıza Hoşgeldiniz, {userName}!</h2>
                        <p>Luxda.net ailesine katıldığınız için çok mutluyuz. Artık ayrıcalıklı alışveriş dünyasının bir parçasısınız.</p>
                        <p>Hesabınız başarıyla oluşturuldu. Hemen alışverişe başlayabilir, size özel kampanyalardan yararlanabilirsiniz.</p>
                        <center>
                            <a href='https://luxda.net' class='button'>Alışverişe Başla</a>
                        </center>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Luxda.net. Tüm hakları saklıdır.</p>
                    </div>
                </div>
            </body>
            </html>";
    }

    public static string GetOrderConfirmationEmailBody(Order order)
    {
        var sb = new StringBuilder();
        var culture = new CultureInfo("tr-TR");

        sb.Append($@"
            <html>
            <head>{_style}</head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Siparişiniz Alındı</h1>
                    </div>
                    <div class='content'>
                        <h2>Teşekkürler Sayın Müşterimiz,</h2>
                        <p>Siparişiniz başarıyla alınmıştır. Aşağıda sipariş detaylarınızı bulabilirsiniz.</p>
                        
                        <div style='background: #f9f9f9; padding: 15px; border-radius: 4px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>Sipariş No:</strong> {order.OrderNumber}</p>
                            <p style='margin: 0;'><strong>Tarih:</strong> {order.OrderDate.ToString("dd MMMM yyyy HH:mm", culture)}</p>
                        </div>

                        <table class='table'>
                            <thead>
                                <tr>
                                    <th>Ürün</th>
                                    <th>Adet</th>
                                    <th style='text-align: right;'>Fiyat</th>
                                </tr>
                            </thead>
                            <tbody>");

        foreach (var item in order.Items)
        {
            sb.Append($@"
                <tr>
                    <td>
                        {item.ProductName}
                        <div style='font-size: 11px; color: #888; margin-top: 4px;'>
                            KDV %{item.VatRate:0}: {item.VatAmount.ToString("C2", culture)}
                        </div>
                    </td>
                    <td>{item.Quantity}</td>
                    <td style='text-align: right;'>{item.Price.ToString("C2", culture)}</td>
                </tr>");
        }

        sb.Append($@"
                            </tbody>
                            <tfoot>
                                <tr>
                                    <td colspan='2' style='text-align: right;'>Ara Toplam:</td>
                                    <td style='text-align: right;'>{order.SubTotalAmount?.ToString("C2", culture) ?? (order.TotalAmount - (order.ShippingFee)).ToString("C2", culture)}</td>
                                </tr>");

        if (order.VatAmount > 0)
        {
            sb.Append($@"
                <tr>
                    <td colspan='2' style='text-align: right;'>Toplam KDV:</td>
                    <td style='text-align: right;'>{order.VatAmount.ToString("C2", culture)}</td>
                </tr>");
        }

        if (order.ShippingFee > 0)
        {
            sb.Append($@"
                <tr>
                    <td colspan='2' style='text-align: right;'>Kargo:</td>
                    <td style='text-align: right;'>{order.ShippingFee.ToString("C2", culture)}</td>
                </tr>");
        }

        if (order.DiscountAmount > 0)
        {
            sb.Append($@"
                <tr>
                    <td colspan='2' style='text-align: right; color: #d9534f;'>İndirim:</td>
                    <td style='text-align: right; color: #d9534f;'>-{order.DiscountAmount?.ToString("C2", culture)}</td>
                </tr>");
        }

        sb.Append($@"
                                <tr class='total-row'>
                                    <td colspan='2' style='text-align: right; border-top: 2px solid #ddd; padding-top: 15px;'>TOPLAM:</td>
                                    <td style='text-align: right; border-top: 2px solid #ddd; padding-top: 15px; color: #C5A059;'>{order.TotalAmount.ToString("C2", culture)}</td>
                                </tr>
                            </tfoot>
                        </table>

                        <p>Siparişinizin durumunu 'Hesabım > Siparişlerim' sayfasından takip edebilirsiniz.</p>
                        
                        <h3>Teslimat Adresi</h3>
                        <p>
                            {order.ShippingAddressDetail}<br>
                            {order.ShippingAddressDistrict}/{order.ShippingAddressCity}
                        </p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Luxda.net. Tüm hakları saklıdır.</p>
                    </div>
                </div>
            </body>
            </html>");

        return sb.ToString();
    }

    public static string GetPasswordResetEmailBody(string resetLink)
    {
        return $@"
            <html>
            <head>{_style}</head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Şifre Sıfırlama</h1>
                    </div>
                    <div class='content'>
                        <h2>Şifrenizi mi Unuttunuz?</h2>
                        <p>Hesabınız için bir şifre sıfırlama talebi aldık. Eğer bu talebi siz yapmadıysanız bu e-postayı dikkate almayınız.</p>
                        <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayınız:</p>
                        <center>
                            <a href='{resetLink}' class='button'>Şifremi Sıfırla</a>
                        </center>
                        <p style='margin-top: 20px; font-size: 12px; color: #999;'>Link 1 saat boyunca geçerlidir.</p>
                    </div>
                     <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Luxda.net. Tüm hakları saklıdır.</p>
                    </div>
                </div>
            </body>
            </html>";
    }
    public static string GetGeneralEmailBody(string title, string content, string buttonText = null, string buttonUrl = null)
    {
        var buttonHtml = "";
        if (!string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonUrl))
        {
            buttonHtml = $@"
                <center>
                    <a href='{buttonUrl}' class='button'>{buttonText}</a>
                </center>";
        }

        return $@"
            <html>
            <head>{_style}</head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h1>Luxda.net</h1>
                    </div>
                    <div class='content'>
                        <h2>{title}</h2>
                        <div style='font-size: 14px; line-height: 1.6; color: #555;'>
                            {content}
                        </div>
                        {buttonHtml}
                    </div>
                     <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Luxda.net. Tüm hakları saklıdır.</p>
                        <p>Bu e-posta hakkında sorularınız varsa lütfen iletişime geçiniz.</p>
                    </div>
                </div>
            </body>
            </html>";
    }
}
