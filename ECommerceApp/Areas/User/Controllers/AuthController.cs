using ECommerceApp.Models.Data;
using ECommerceApp.Models.ViewModels;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Security.Claims;

namespace ECommerceApp.Areas.User.Controllers;

[Area("User")]
public class AuthController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IToastNotification _toastNotification;
    private readonly ICartService _cartService;
    private readonly IEmailService _emailService;

    public AuthController(ApplicationDbContext context, IToastNotification toastNotification, ICartService cartService, IEmailService emailService)
    {
        _context = context;
        _toastNotification = toastNotification;
        _cartService = cartService;
        _emailService = emailService;
    }

    [Route("/giris")]
    public IActionResult Index(string returnUrl = null)
    {
        if (User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.ReturnUrl = returnUrl;
        return View(new AuthViewModel());
    }

    [HttpPost]
    [Route("/giris")]
    public async Task<IActionResult> Login([Bind(Prefix = "Login")] LoginVM model, string returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View("Index", new AuthViewModel { Login = model });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
        {
            _toastNotification.AddErrorToastMessage("E-posta veya şifre hatalı!", new ToastrOptions { Title = "Hata" });
            ViewBag.ReturnUrl = returnUrl;
            return View("Index", new AuthViewModel { Login = model });
        }

        if (!user.IsActive)
        {
            _toastNotification.AddErrorToastMessage("Hesabınız aktif değil!", new ToastrOptions { Title = "Hata" });
            ViewBag.ReturnUrl = returnUrl;
            return View("Index", new AuthViewModel { Login = model });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Merge session cart to user cart
        await _cartService.MergeSessionCartToUserCartAsync(HttpContext, user.Id);


        // Redirect based on role
        if (user.Role == "Admin")
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        // Redirect to return URL if provided and is local
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }


    [HttpPost]
    [Route("/kayit-ol")]
    public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterVM model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new AuthViewModel { Register = model });
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
        if (existingUser != null)
        {
            _toastNotification.AddErrorToastMessage("Bu e-posta adresi zaten kayıtlı!", new ToastrOptions { Title = "Hata" });
            return View("Index", new AuthViewModel { Register = model });
        }

        // Check if this is the first user (make them admin)
        var userCount = await _context.Users.CountAsync();
        var userRole = userCount == 0 ? "Admin" : "User";

        var user = new Models.User
        {
            Name = model.Name,
            Surname = model.Surname,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Role = userRole,
            CreatedDate = DateTime.Now,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Auto-login after registration
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.Name} {user.Surname}"),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Merge session cart to user cart
        await _cartService.MergeSessionCartToUserCartAsync(HttpContext, user.Id);

        // Send Welcome Email
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, $"{user.Name} {user.Surname}");
        }
        catch (Exception ex)
        {
            // Email sending failed, but registration was successful. 
            // We might want to log this or silently fail to not disrupt the user flow.
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            _toastNotification.AddErrorToastMessage("Lütfen e-posta adresinizi giriniz.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Güvenlik gereği kullanıcı bulunamadı demek yerine başarılı gibi davranabiliriz veya hata dönebiliriz.
            _toastNotification.AddErrorToastMessage("Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }

        // Token oluştur
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpires = DateTime.Now.AddHours(1); // 1 saat geçerli

        _context.Update(user);
        await _context.SaveChangesAsync();

        // Mail gönder
        var resetLink = Url.Action("ResetPassword", "Auth", new { token = token, email = email }, Request.Scheme);
        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        _toastNotification.AddSuccessToastMessage("Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.", new ToastrOptions { Title = "Başarılı" });
        return RedirectToAction("Index");
    }

    [HttpGet]
    [Route("sifre-sifirla")]
    public async Task<IActionResult> ResetPassword(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            _toastNotification.AddErrorToastMessage("Geçersiz şifre sıfırlama bağlantısı.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordResetToken == token);
        if (user == null || user.PasswordResetTokenExpires < DateTime.Now)
        {
            _toastNotification.AddErrorToastMessage("Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }

        return View(new ResetPasswordVM { Token = token, Email = email });
    }

    [HttpPost]
    [Route("sifre-sifirla")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.PasswordResetToken == model.Token);
        if (user == null || user.PasswordResetTokenExpires < DateTime.Now)
        {
            _toastNotification.AddErrorToastMessage("İşlem başarısız. Lütfen tekrar deneyin.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction("Index");
        }

        // Şifreyi güncelle
        user.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;

        _context.Update(user);
        await _context.SaveChangesAsync();

        _toastNotification.AddSuccessToastMessage("Şifreniz başarıyla güncellendi. Giriş yapabilirsiniz.", new ToastrOptions { Title = "Başarılı" });
        return RedirectToAction("Index");
    }

}