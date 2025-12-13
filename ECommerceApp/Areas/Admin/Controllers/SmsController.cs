using ECommerceApp.Models.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SmsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly IToastNotification _toastNotification;

        public SmsController(ApplicationDbContext context, ISmsService smsService, IToastNotification toastNotification)
        {
            _context = context;
            _smsService = smsService;
            _toastNotification = toastNotification;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Send(string message, bool sendToAllUsers, string? specificNumbers)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _toastNotification.AddErrorToastMessage("Lütfen bir mesaj giriniz.", new ToastrOptions { Title = "Hata" });
                return View("Index");
            }

            var phoneNumbers = new List<string>();

            // 1. Specific Numbers
            if (!string.IsNullOrWhiteSpace(specificNumbers))
            {
                var split = specificNumbers.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in split)
                {
                    var clean = s.Trim();
                    if (!string.IsNullOrEmpty(clean)) phoneNumbers.Add(clean);
                }
            }

            // 2. All Users
            if (sendToAllUsers)
            {
                var userPhones = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.PhoneNumber))
                    .Select(u => u.PhoneNumber)
                    .ToListAsync();

                phoneNumbers.AddRange(userPhones!);
            }

            phoneNumbers = phoneNumbers.Distinct().ToList();

            if (!phoneNumbers.Any())
            {
                _toastNotification.AddWarningToastMessage("Gönderilecek telefon numarası bulunamadı.", new ToastrOptions { Title = "Uyarı" });
                return View("Index");
            }

            bool result = await _smsService.SendSmsAsync(message, phoneNumbers);

            if (result)
            {
                _toastNotification.AddSuccessToastMessage($"{phoneNumbers.Count} numaraya SMS başarıyla gönderildi (veya işlem başlatıldı).", new ToastrOptions { Title = "Başarılı" });
            }
            else
            {
                _toastNotification.AddErrorToastMessage("SMS gönderimi başarısız oldu. Logları kontrol ediniz.", new ToastrOptions { Title = "Hata" });
            }

            return RedirectToAction("Index");
        }
    }
}
