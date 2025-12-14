# 🛒 ASP.NET Core 9.0 Gelişmiş E-Ticaret Platformu

![.NET](https://img.shields.io/badge/.NET-9.0-purple) ![EF Core](https://img.shields.io/badge/EF%20Core-9.0-blue) ![Hangfire](https://img.shields.io/badge/Hangfire-Background%20Jobs-red) ![Status](https://img.shields.io/badge/Status-Production%20Ready-success)

## 📖 Genel Bakış
Bu proje, güncel **ASP.NET Core 9.0** teknolojisi ile geliştirilmiş, uçtan uca, prodüksiyon ortamına hazır bir **E-Ticaret Altyapısıdır**. Karmaşık iş mantıklarını, otomatikleştirilmiş XML ürün entegrasyonlarını, çoklu ödeme sistemlerini ve kargo süreçlerini tek bir çatı altında yönetmek üzere tasarlanmıştır.

> **İnceleyenler İçin Not:** Bu depo, sadece bir ön yüz uygulaması değil; **Arkaplan İşlemleri (Background Jobs)** ve **Üçüncü Parti API Entegrasyonları** (PayTR, Iyzico, Navlungo, NetGSM) içeren kapsamlı bir e-ticaret platformu örneğidir.

---

🔐 Admin Panel & Erişim Bilgileri  
Not: Bu bilgiler demo / inceleme amaçlıdır. Canlı projelerde mutlaka değiştirilmelidir.

🌐 Site URL  
https://heuristic-satoshi.104-247-162-242.plesk.page/

🧩 Admin Panel URL  
https://heuristic-satoshi.104-247-162-242.plesk.page/admin/dashboard

👤 Admin Giriş Bilgileri  
Kullanıcı Adı: admin@gmail.com  
Şifre: 123456

## 🚀 Temel Teknik Özellikler
### 1. 🔄 Gelişmiş XML Ürün Entegrasyonu (Hangfire)
Sistem, her gece **04:00**'da çalışan bir "Job" motoruna sahiptir.
*   **Çoklu Tedarikçi Desteği:** Farklı şemalara sahip tedarikçilerden gelen verileri normalize eder.
*   **Performans Optimizasyonu:**
    *   **Batch Processing (Toplu İşleme):** Bellek yönetimini optimize etmek için ürünler 100'erli paketler halinde işlenir.
    *   **Akıllı Caching:** Kategori ve Marka sorguları işlem süresince bellekte (Dictionary) tutularak veritabanı trafiği azaltılmıştır.
    *   **Transaction Yönetimi:** Hata durumunda sadece ilgili paketi geri alır (Rollback), tüm süreci bozmaz.
*   **Stok Koruması:** Stok adedi kritik seviyenin (`<= 2`) altındaki ürünler otomatik olarak satışa kapatılır.

### 2. 💳 Ödeme Altyapısı
*   **Hibrit Ödeme Ağ Geçidi:** Admin panelinden tek tuşla **PayTR** veya **Iyzico** altyapısına geçiş yapılabilir.
*   **Güvenlik:** PayTR için iFrame API, Iyzico için güvenli form yapısı entegre edilmiştir.
*   **Ödeme Bildirimi:** Eft/Havale ile ödemelerde kullanıcı satıcının banka hesap bilgilerini görüntüleyip siparişi için ödeme bildiriminde bulunabilir.

### 3. 📦 Lojistik ve Kargo (Navlungo)
*   **Navlungo API** entegrasyonu ile sepet aşamasında gerçek zamanlı kargo maliyeti hesaplanır.
*   Sipariş sonrası otomatik kargo barkodu oluşturulur.

### 4. 📲 Bildirim Sistemi (SMS & Email)
*   **NetGSM Entegrasyonu:** Müşterilere sipariş durumları hakkında XML tabanlı API üzerinden SMS bilgilendirmesi yapılır.
*   **Dinamik SMTP Motoru:** Mail sunucu ayarları (Host, Port, Credentials) `appsettings.json` yerine veritabanında tutulur; böylece kod değiştirmeden sunucu değişikliği yapılabilir.
*   **Şablon Motoru:** *Hoşgeldin*, *Sipariş Onayı* ve *Şifre Sıfırlama* gibi mailler HTML şablonları üzerinden dinamik olarak oluşturulur.

### 🧾 Akakçe & Cimri Ürün Feed Sistemi
*   Fiyat karşılaştırma platformları olan Akakçe ve Cimri için XML tabanlı ürün feed altyapısı geliştirilmiştir.
*   Bu yapı sayesinde ürünler, platformların istediği formatta otomatik olarak dış sistemlere aktarılır.

📌 Feed URL’leri
/feed/akakce.xml
/feed/cimri.xml
* Dinamik XML Üretimi: Feed’ler anlık olarak veritabanından üretilir, statik dosya kullanılmaz.
* Platforma Özel Şema: Akakçe ve Cimri’nin XML standartlarına uygun alan eşleştirmeleri yapılmıştır.
* Stok & Fiyat Kontrolü:
* Stokta olmayan ürünler otomatik olarak feed dışında bırakılır.
* Güncel fiyat, indirimli fiyat ve KDV dahil tutarlar doğru şekilde yansıtılır.
* SEO & Kategori Uyumlu: Ürün URL’leri SEO uyumlu slug yapısı ile feed’e eklenir.
* Performans Odaklı: Büyük ürün sayılarında dahi hızlı üretim için optimize edilmiştir.
* Canlı Güncelleme: Ürün fiyatı veya stok değiştiğinde feed otomatik olarak güncel kalır.

---

## 📂 Proje Yapısı

```
ECommerceApp/
├── Services/               # İş Mantığı Katmanı (Business Logic)
│   ├── XmlImportService.cs # XML İşleme, Batching ve Transaction Mantığı
│   ├── NetGsmSmsService.cs # SOAP/XML SMS Entegrasyonu
│   ├── EmailService.cs     # Dinamik SMTP Servisi
│   └── ...
├── Models/                 # EF Core Varlıkları (Entities)
├── Controllers/            # MVC Controller'lar
├── Views/                  # Razor Arayüzleri (Martfury Teması)
└── Program.cs              # DI Container & Hangfire Konfigürasyonu
```

---

## 🛠️ Kurulum

### Gereksinimler
*   .NET 9.0 SDK
*   SQL Server (2019 veya üzeri)

### Adımlar
1.  **Projeyi Klonlayın**
    ```bash
    git clone https://github.com/kullaniciadiniz/luxda-commerce.git
    ```
2.  **Ayarları Yapılandırın**
    `appsettings.json` dosyasındaki Connection String alanını kendi sunucunuza göre düzenleyin.
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=...;Database=...;"
    }
    ```
3.  **Veritabanını Oluşturun**
    ```bash
    dotnet ef database update
    ```
4.  **Uygulamayı Başlatın**
    ```bash
    dotnet run
    ```

---

## 🛠️ Teknolojiler ve Teknik Altyapı
Bu proje, modern ve ölçeklenebilir e-ticaret ihtiyaçlarını karşılamak üzere .NET 9 altyapısı üzerine inşa edilmiştir.

### Backend
*   .NET 9.0 (ASP.NET Core MVC)
*   Veritabanı: Microsoft SQL Server (MSSQL)
*   ORM: Entity Framework Core 9 (Code-First yaklaşımı)
*   In-Memory Cache
*   Hangfire (SQL Server depolama ile XML import süreçleri ve zamanlanmış görevler için)
*   Mapping: AutoMapper
*   Logging: Özel Memory Logger ve Serilog altyapısı

### Frontend 
*   Template: Martfury E-Ticaret Teması
*   CSS Framework: Bootstrap
*   JavaScript Kütüphaneleri: jQuery 3.7.1,  SweetAlert2 (Modern pop-up bildirimleri için), Owl Carousel & Slick Slider (Slider bileşenleri için) .....
*   Bildirimler: NToastNotify (Toastr bildirimleri)

### Entegrasyonlar ve Servisler
Proje, tam fonksiyonel bir e-ticaret deneyimi sunmak için çeşitli 3. parti servislerle entegre çalışır:

### Ödeme Sistemleri:
*   Iyzico: Kredi kartı ile güvenli ödeme altyapısı.
*   PayTR: Alternatif sanal POS entegrasyonu.
*   Kargo & Lojistik:  vlungo: Kargo fiyat hesaplama ve gönderi takibi entegrasyonu.
*   İletişim & SMS:  tGSM: Sipariş ve durum bildirimleri için SMS servisi.
*   XML Entegrasyonları: Akakçe ve Cimri gibi fiyat karşılaştırma siteleri için otomatik XML feed oluşturma.
*   Ürün Tedariği: Tedarikçilerden otomatik ürün ve stok güncellemek için gelişmiş XML Import servisi.
*   MVC (Model-View-Controller): Projenin temel mimari yapısı.
*   Dependency Injection (DI): Servislerin (Email, SMS, Kargo vb.) gevşek bağımlılıkla yönetilmesi.
*   Dinamik Sitemap.xml ve Robots.txt yönetimi.
*   Akıllı URL yapısı (Slugify) - Örn: /kategori/telefon-kiliflari-123.

### Kullanılan Önemli NuGet Paketleri
*   Hangfire - Arka plan işleri yönetimi.
*   HtmlAgilityPack & HtmlSanitizer - HTML işleme ve XSS koruması.
*   ClosedXML - Excel raporlama ve veri dışa aktarma.
*   BCrypt.Net-Next - Güvenli şifreleme.
*   Iyzipay - Iyzico resmi kütüphanesi.
*   X.PagedList.Mvc.Core - Sayfalama (Pagination) altyapısı.

## 👨‍💻 Yazar
**Selçuk Mehmet TUNÇER**
*.NET Developer*

