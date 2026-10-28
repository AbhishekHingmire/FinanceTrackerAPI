namespace FinanceTrackerAPI.Services.Implementations;

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;
using FinanceTrackerAPI.Services.Interfaces;

public class OtpService : IOtpService
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<OtpService> _logger;
    private const int OtpValidityMinutes = 5;
    private const int MaxAttempts = 3;

    public OtpService(FinanceTrackerDbContext dbContext, ILogger<OtpService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(string OtpCode, DateTimeOffset ExpiresAt)> GenerateAndStoreOtpAsync(
        string mobileNumber,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        // Generate cryptographically secure 6-digit random number (100000 - 999999)
        var randomCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(OtpValidityMinutes);

        // Invalidate any existing unused OTPs for this mobile
        var existingOtps = await _dbContext.UserOtps
            .Where(o => o.MobileNumber == mobileNumber && !o.IsUsed && o.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        foreach (var old in existingOtps)
        {
            old.IsUsed = true;
        }

        var otpRecord = new UserOtp
        {
            MobileNumber = mobileNumber,
            OtpHash = HashOtp(randomCode, mobileNumber),
            Purpose = purpose,
            ExpiresAt = expiresAt,
            AttemptCount = 0,
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.UserOtps.Add(otpRecord);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Generated random OTP for mobile {MobileNumber}, valid until {ExpiresAt}", mobileNumber, expiresAt);

        return (randomCode, expiresAt);
    }

    public async Task<bool> VerifyOtpAsync(string mobileNumber, string otpCode, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var hashedOtp = HashOtp(otpCode, mobileNumber);

        var record = await _dbContext.UserOtps
            .Where(o => o.MobileNumber == mobileNumber && !o.IsUsed && o.ExpiresAt > now)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (record == null)
        {
            _logger.LogWarning("No active OTP found or OTP expired for mobile {MobileNumber}", mobileNumber);
            return false;
        }

        if (record.AttemptCount >= MaxAttempts)
        {
            record.IsUsed = true;
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogWarning("Max OTP attempts exceeded for mobile {MobileNumber}", mobileNumber);
            return false;
        }

        record.AttemptCount++;

        // Constant-time comparison to prevent timing attacks
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(record.OtpHash),
            Encoding.UTF8.GetBytes(hashedOtp));

        if (isValid)
        {
            record.IsUsed = true;
            _logger.LogInformation("OTP successfully verified for mobile {MobileNumber}", mobileNumber);
        }
        else
        {
            _logger.LogWarning("Invalid OTP entered for mobile {MobileNumber}, attempt {AttemptCount}/{MaxAttempts}",
                mobileNumber, record.AttemptCount, MaxAttempts);
        }

        await _dbContext.SaveChangesAsync(ct);
        return isValid;
    }

    private static string HashOtp(string otp, string mobileNumber)
    {
        var combined = $"{mobileNumber}:{otp}:WT_SALT_2026";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes);
    }
}
