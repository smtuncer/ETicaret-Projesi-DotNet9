using ECommerceApp.Models.ViewModels;

namespace ECommerceApp.Services;

public interface ILogService
{
    /// <summary>
    /// Log kayıtlarını filtreler ve döndürür
    /// </summary>
    Task<LogViewVM> GetLogsAsync(LogFilterVM filter);

    /// <summary>
    /// Tüm log kayıtlarını temizler
    /// </summary>
    Task ClearLogsAsync();

    /// <summary>
    /// Log kayıtlarını dışa aktarır
    /// </summary>
    Task<string> ExportLogsAsync(LogFilterVM filter);
}

