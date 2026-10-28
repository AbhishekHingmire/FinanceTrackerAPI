namespace FinanceTrackerAPI.Services.Interfaces;

using FinanceTrackerAPI.Enums;

public interface IOtpService
{
    Task<(string OtpCode, DateTimeOffset ExpiresAt)> GenerateAndStoreOtpAsync(string mobileNumber, OtpPurpose purpose, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(string mobileNumber, string otpCode, CancellationToken ct = default);
}
