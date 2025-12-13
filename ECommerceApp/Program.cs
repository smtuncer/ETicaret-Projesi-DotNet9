using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NToastNotify;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        // Login/Logout Paths
        opt.LoginPath = "/giris";  // User login page
        opt.AccessDeniedPath = "/hata";

        // Timeout Settings - UZUN SÜRELİ OTURUM
        opt.ExpireTimeSpan = TimeSpan.FromHours(8); // 8 saatlik oturum süresi
        opt.SlidingExpiration = true; // Kullanıcı aktifken süre yenilenir

        // Cookie Settings - GÜVENLİK
        opt.Cookie.HttpOnly = true; // JavaScript erişimini engelle
        opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // HTTPS desteği
        opt.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax; // CSRF koruması
        opt.Cookie.IsEssential = true; // Cookie kabul edilmesi zorunlu
        opt.Cookie.Name = "EComAuth"; // Özel cookie adı
    });

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 52428800; // 50 MB
});

// Services
builder.Services.AddScoped<ISiteContentService, SiteContentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IXmlImportService, XmlImportService>();
builder.Services.AddScoped<ISeoService, SeoService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddScoped<IPayTrService, PayTrService>();
builder.Services.AddScoped<IIyzicoService, IyzicoService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderCalculationService, OrderCalculationService>();
builder.Services.AddScoped<ISmsService, NetGsmSmsService>();
builder.Services.AddScoped<INavlungoService, NavlungoService>();

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// Custom Memory Logger Provider
builder.Logging.AddProvider(new MemoryLoggerProvider());

var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;


ConfigurationManager configuration = builder.Configuration;
IWebHostEnvironment environment = builder.Environment;

builder.Services.AddControllersWithViews();
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // 60 dakika
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddMvc().AddNToastNotifyToastr(new ToastrOptions()
{
    ProgressBar = false,
    PositionClass = ToastPositions.BottomRight
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/hata/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/hata/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 30; // 30 gün
        ctx.Context.Response.Headers[HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});

app.UseSession();

app.UseRouting();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("tr-TR"),
});

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Admin only)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Schedule Daily XML Import at 04:00 AM (local time, or server time depending on config, usually UTC default but Cron handles it)
// Using Cron.Daily(4) means 04:00 am.
// Note: Ensure the timezone is correct if needed. Default is UTC. If user is in Turkey (UTC+3), 04:00 TRT is 01:00 UTC.
// However, Cron.Daily(4) usually implies 04:00 server local time if not specified otherwise, but Hangfire defaults to UTC.
// Let's assume server is UTC or we want 4 AM local.
// Safest is to specify TimeZoneInfo if possible, but standard RecurringJob.AddOrUpdate overload with cron uses UTC by default in newer versions.
// If the user said "Her gün sabah 04:00'da", assuming Turkey (UTC+3), that is 01:00 UTC.
// Let's use 04:00 and assume they might adjust if server time differs, or try to use TZ.
RecurringJob.AddOrUpdate<IXmlImportService>(
    "daily-xml-import",
    service => service.ImportAllProductsAsync(),
    "0 4 * * *", // 04:00
    TimeZoneInfo.Local // Use local time of the server
);

app.UseNToastNotify();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "category_seo",
    pattern: "kategori/{categorySlug}-{categoryId}",
    defaults: new { area = "User", controller = "Product", action = "Index" },
    constraints: new { categoryId = @"\d+" });

app.MapControllerRoute(
    name: "default",
    pattern: "{area=User}/{controller=Home}/{action=Index}/{id?}");

app.Run();
