namespace FinanceTrackerAPI.Entities;

public class ErrorLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string LogLevel { get; set; } = "Error";
    public string? ControllerName { get; set; }
    public string? MethodName { get; set; }
    public string? ExceptionType { get; set; }
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public string? IpAddress { get; set; }
    public string? AdditionalInfo { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Optional navigation to User
    public virtual User? User { get; set; }
}
