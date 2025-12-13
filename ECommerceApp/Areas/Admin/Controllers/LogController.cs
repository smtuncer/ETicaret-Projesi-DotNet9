using ECommerceApp.Models.ViewModels;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;
using System.Text;

namespace ECommerceApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LogController : Controller
{
    private readonly ILogService _logService;
    private readonly IToastNotification _toast;
    private readonly ILogger<LogController> _logger;

    public LogController(ILogService logService, IToastNotification toast, ILogger<LogController> logger)
    {
        _logService = logService;
        _toast = toast;
        _logger = logger;
    }

    [Route("admin/log-kayitlari")]
    public async Task<IActionResult> Index(LogFilterVM filter)
    {
        try
        {
            var viewModel = await _logService.GetLogsAsync(filter);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading log page");
            _toast.AddErrorToastMessage("Log kayıtları yüklenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return View(new LogViewVM());
        }
    }

    [HttpPost]
    [Route("admin/log-kayitlari/temizle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearLogs()
    {
        try
        {
            await _logService.ClearLogsAsync();
            _toast.AddSuccessToastMessage("Tüm log kayıtları başarıyla temizlendi.", new ToastrOptions { Title = "Başarılı" });
            _logger.LogWarning("All logs cleared by admin user: {Username}", User.Identity?.Name);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while clearing logs");
            _toast.AddErrorToastMessage("Log kayıtları temizlenirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }
    }

    [Route("admin/log-kayitlari/indir")]
    public async Task<IActionResult> DownloadLogs(LogFilterVM filter)
    {
        try
        {
            var csvContent = await _logService.ExportLogsAsync(filter);
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            var fileName = $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while downloading logs");
            _toast.AddErrorToastMessage("Log kayıtları indirilirken bir hata oluştu.", new ToastrOptions { Title = "Hata" });
            return RedirectToAction(nameof(Index));
        }
    }
}

