namespace ECommerceApp.Models.ViewModels;

public class LogEntryVM
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public string? Source { get; set; }
    public string? Category { get; set; }
}

public class LogFilterVM
{
    public string? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class LogViewVM
{
    public List<LogEntryVM> Logs { get; set; } = new();
    public LogFilterVM Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, int> LogLevelCounts { get; set; } = new();
}

