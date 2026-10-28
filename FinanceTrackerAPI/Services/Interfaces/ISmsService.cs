namespace FinanceTrackerAPI.Services.Interfaces;

public class SmsSendResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? GatewayStatus { get; set; }
    public string? RawResponse { get; set; }
}

public interface ISmsService
{
    Task<SmsSendResult> SendOtpSmsAsync(string mobileNumber, string otpCode, CancellationToken ct = default);
}
