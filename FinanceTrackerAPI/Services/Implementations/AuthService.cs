namespace FinanceTrackerAPI.Services.Implementations;

using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Auth;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;
using FinanceTrackerAPI.Services.Interfaces;

public class AuthService : IAuthService
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private const int RefreshTokenExpiryDays = 30;

    public AuthService(
        FinanceTrackerDbContext dbContext,
        IOtpService otpService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<ApiResponse<SendOtpResponseDto>> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default)
    {
        var normalizedMobile = request.MobileNumber.Trim();
        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MobileNumber == normalizedMobile, ct);

        var isRegistered = existingUser != null;
        var purpose = isRegistered ? OtpPurpose.Login : OtpPurpose.Signup;

        var (otpCode, expiresAt) = await _otpService.GenerateAndStoreOtpAsync(normalizedMobile, purpose, ct);

        var response = new SendOtpResponseDto
        {
            MobileNumber = normalizedMobile,
            ExpiresInSeconds = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds,
            IsRegisteredUser = isRegistered,
            // Returned for web UI testing as requested; will be suppressed when SMS gateway is attached
            TestOtp = otpCode
        };

        var message = isRegistered
            ? "Login OTP generated successfully."
            : "Signup OTP generated successfully.";

        return ApiResponse<SendOtpResponseDto>.SuccessResult(response, message);
    }

    public async Task<ApiResponse<AuthResponseDto>> VerifyOtpAndAuthenticateAsync(
        VerifyOtpRequestDto request,
        CancellationToken ct = default)
    {
        var normalizedMobile = request.MobileNumber.Trim();
        var normalizedOtp = request.Otp.Trim();

        var isOtpValid = await _otpService.VerifyOtpAsync(normalizedMobile, normalizedOtp, ct);
        if (!isOtpValid)
        {
            return ApiResponse<AuthResponseDto>.FailureResult("Invalid or expired OTP. Please check and try again.");
        }

        var user = await _dbContext.Users
            .Include(u => u.ProfileTargets)
            .Include(u => u.WealthPreferences)
            .FirstOrDefaultAsync(u => u.MobileNumber == normalizedMobile, ct);

        var isNewUser = false;

        if (user == null)
        {
            // Registration flow
            isNewUser = true;
            var fullName = string.IsNullOrWhiteSpace(request.FullName)
                ? $"User {normalizedMobile[^4..]}"
                : request.FullName.Trim();

            user = new User
            {
                MobileNumber = normalizedMobile,
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
                IsMobileVerified = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(ct);

            // Seed initial categories for new user
            SeedUserDefaultCategories(user.Id);

            // Seed default profile & preferences
            user.ProfileTargets = new UserProfileTargets
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                MonthlyIncomeTarget = 10000000, // ₹1,00,000 in Paise
                SavingsRateTarget = 35,
                EmergencyTargetMonths = 6,
                RiskProfile = "Balanced"
            };

            user.WealthPreferences = new UserWealthPreferences
            {
                UserId = user.Id,
                Currency = "INR",
                DateFormat = "DD/MM/YYYY",
                WeekStart = "Monday",
                Theme = "light",
                DefaultType = "Expense",
                DefaultPaymentMethod = "UPI"
            };

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("New user registered successfully: {UserId}, Mobile: {Mobile}", user.Id, user.MobileNumber);
        }
        else
        {
            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.FailureResult("This user account has been deactivated. Please contact support.");
            }

            user.IsMobileVerified = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.FullName) && string.IsNullOrWhiteSpace(user.FullName))
            {
                user.FullName = request.FullName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(user.Email))
            {
                user.Email = request.Email.Trim().ToLowerInvariant();
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        // Issue Tokens
        var (accessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = rawRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(ct);

        var authResponse = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = accessExpiresAt,
            IsNewUser = isNewUser,
            User = new UserDto
            {
                Id = user.Id,
                MobileNumber = user.MobileNumber,
                FullName = user.FullName,
                Email = user.Email,
                IsMobileVerified = user.IsMobileVerified,
                CreatedAt = user.CreatedAt
            }
        };

        var message = isNewUser
            ? "Account created successfully. Welcome to WealthTrack!"
            : "Login successful. Welcome back!";

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, message);
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken ct = default)
    {
        var rawToken = request.RefreshToken.Trim();

        var tokenRecord = await _dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == rawToken, ct);

        if (tokenRecord == null || tokenRecord.IsRevoked || tokenRecord.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return ApiResponse<AuthResponseDto>.FailureResult("Invalid or expired refresh token. Please sign in again.");
        }

        var user = tokenRecord.User;
        if (!user.IsActive)
        {
            return ApiResponse<AuthResponseDto>.FailureResult("User account is inactive.");
        }

        // Token Rotation: revoke old token and issue new pair
        tokenRecord.IsRevoked = true;
        var (newAccessToken, accessExpiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        tokenRecord.ReplacedByToken = newRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(ct);

        var authResponse = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = accessExpiresAt,
            IsNewUser = false,
            User = new UserDto
            {
                Id = user.Id,
                MobileNumber = user.MobileNumber,
                FullName = user.FullName,
                Email = user.Email,
                IsMobileVerified = user.IsMobileVerified,
                CreatedAt = user.CreatedAt
            }
        };

        return ApiResponse<AuthResponseDto>.SuccessResult(authResponse, "Token refreshed successfully.");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var token = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Token == refreshToken, ct);

            if (token != null)
            {
                token.IsRevoked = true;
                await _dbContext.SaveChangesAsync(ct);
            }
        }
        else
        {
            // Revoke all active refresh tokens for this user
            var tokens = await _dbContext.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync(ct);

            foreach (var t in tokens)
            {
                t.IsRevoked = true;
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        return ApiResponse<bool>.SuccessResult(true, "Logged out successfully.");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return ApiResponse<UserDto>.FailureResult("User not found.");
        }

        var dto = new UserDto
        {
            Id = user.Id,
            MobileNumber = user.MobileNumber,
            FullName = user.FullName,
            Email = user.Email,
            IsMobileVerified = user.IsMobileVerified,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserDto>.SuccessResult(dto);
    }

    private void SeedUserDefaultCategories(Guid userId)
    {
        var presets = new List<Category>
        {
            new() { UserId = userId, Name = "Food & dining", Kind = EntryKind.Expense, Icon = "🍽️", Color = "rose", MonthlyCap = 1200000, DefaultNeedWant = NeedWant.Need },
            new() { UserId = userId, Name = "Housing & Rent", Kind = EntryKind.Expense, Icon = "🏠", Color = "blue", MonthlyCap = 2500000, DefaultNeedWant = NeedWant.Need },
            new() { UserId = userId, Name = "Transport & Fuel", Kind = EntryKind.Expense, Icon = "🚗", Color = "teal", MonthlyCap = 600000, DefaultNeedWant = NeedWant.Need },
            new() { UserId = userId, Name = "Shopping & Personal", Kind = EntryKind.Expense, Icon = "🛍️", Color = "violet", MonthlyCap = 800000, DefaultNeedWant = NeedWant.Want },
            new() { UserId = userId, Name = "Bills & Utilities", Kind = EntryKind.Expense, Icon = "💡", Color = "amber", MonthlyCap = 700000, DefaultNeedWant = NeedWant.Need },
            new() { UserId = userId, Name = "Healthcare", Kind = EntryKind.Expense, Icon = "🏥", Color = "green", MonthlyCap = 500000, DefaultNeedWant = NeedWant.Need },
            new() { UserId = userId, Name = "Entertainment", Kind = EntryKind.Expense, Icon = "🎬", Color = "pink", MonthlyCap = 400000, DefaultNeedWant = NeedWant.Want },
            new() { UserId = userId, Name = "Salary", Kind = EntryKind.Income, Icon = "💼", Color = "green", MonthlyCap = null },
            new() { UserId = userId, Name = "Freelance & Consulting", Kind = EntryKind.Income, Icon = "💻", Color = "blue", MonthlyCap = null },
            new() { UserId = userId, Name = "Dividends & Returns", Kind = EntryKind.Income, Icon = "📈", Color = "teal", MonthlyCap = null },
            new() { UserId = userId, Name = "Loan EMI & Debt", Kind = EntryKind.Expense, Icon = "💳", Color = "rose", MonthlyCap = 4500000, DefaultNeedWant = NeedWant.Need }
        };

        _dbContext.Categories.AddRange(presets);
    }
}
