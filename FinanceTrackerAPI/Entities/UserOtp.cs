namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class UserOtp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MobileNumber { get; set; } = string.Empty;
    public string OtpHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
