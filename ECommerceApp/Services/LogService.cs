using ECommerceApp.Models.ViewModels;
using System.Collections.Concurrent;
using System.Text;

namespace ECommerceApp.Services;

public class LogService : ILogService
{
    private static readonly ConcurrentQueue<LogEntryVM> LogEntries = new();
    private const int MaxLogEntries = 10000; // Maksimum 10000 log kaydı tutulur
    private readonly ILogger<LogService> _logger;

    public LogService(ILogger<LogService> logger)
    {
        _logger = logger;
    }

    public static void AddLogEntry(LogEntryVM logEntry)
    {
        LogEntries.Enqueue(logEntry);

        // Maksimum log sayısını aşarsa eski logları temizle
        while (LogEntries.Count > MaxLogEntries)
        {
            LogEntries.TryDequeue(out _);
        }
    }

    public Task<LogViewVM> GetLogsAsync(LogFilterVM filter)
    {
        try
        {
            var allLogs = LogEntries.ToList();
            var filteredLogs = allLogs.AsEnumerable();

            // Filtrele
            if (!string.IsNullOrEmpty(filter.Level))
            {
                filteredLogs = filteredLogs.Where(l => l.Level.Equals(filter.Level, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.StartDate.HasValue)
            {
                filteredLogs = filteredLogs.Where(l => l.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                var endDate = filter.EndDate.Value.AddDays(1); // Günün sonuna kadar
                filteredLogs = filteredLogs.Where(l => l.Timestamp < endDate);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchLower = filter.SearchTerm.ToLower();
                filteredLogs = filteredLogs.Where(l =>
                    l.Message.ToLower().Contains(searchLower) ||
                    (l.Exception != null && l.Exception.ToLower().Contains(searchLower)) ||
                    (l.Source != null && l.Source.ToLower().Contains(searchLower))
                );
            }

            // Sıralama (en yeni önce)
            filteredLogs = filteredLogs.OrderByDescending(l => l.Timestamp);

            var totalCount = filteredLogs.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);

            // Sayfalama
            var pagedLogs = filteredLogs
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            // Log seviye sayıları
            var logLevelCounts = allLogs
                .GroupBy(l => l.Level)
                .ToDictionary(g => g.Key, g => g.Count());

            return Task.FromResult(new LogViewVM
            {
                Logs = pagedLogs,
                Filter = filter,
                TotalCount = totalCount,
                TotalPages = totalPages,
                LogLevelCounts = logLevelCounts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving logs");
            return Task.FromResult(new LogViewVM());
        }
    }

    public Task ClearLogsAsync()
    {
        try
        {
            LogEntries.Clear();
            _logger.LogInformation("All log entries cleared by admin");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while clearing logs");
            throw;
        }
    }

    public Task<string> ExportLogsAsync(LogFilterVM filter)
    {
        try
        {
            var logsResult = GetLogsAsync(filter).Result;
            var sb = new StringBuilder();

            sb.AppendLine("Timestamp,Level,Message,Source,Exception");

            foreach (var log in logsResult.Logs)
            {
                sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.Level}\",\"{EscapeCsv(log.Message)}\",\"{EscapeCsv(log.Source ?? "")}\",\"{EscapeCsv(log.Exception ?? "")}\"");
            }

            return Task.FromResult(sb.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while exporting logs");
            throw;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "");
    }
}

