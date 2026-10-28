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
    private readonly ISmsService _smsService;
    private readonly ILogger<AuthService> _logger;
    private const int RefreshTokenExpiryDays = 30;

    public AuthService(
        FinanceTrackerDbContext dbContext,
        IOtpService otpService,
        IJwtTokenService jwtTokenService,
        ISmsService smsService,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _jwtTokenService = jwtTokenService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task<ApiResponse<SendOtpResponseDto>> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default)
    {
        var normalizedMobile = request.MobileNumber.Trim();
        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.MobileNumber == normalizedMobile, ct);

        var isRegistered = existingUser != null;
        var isLoginRequest = request.Purpose?.Equals("login", StringComparison.OrdinalIgnoreCase) == true;
        var isSignupRequest = request.Purpose?.Equals("signup", StringComparison.OrdinalIgnoreCase) == true;

        // Strict business validation: Only registered users can request a login OTP
        if (isLoginRequest && !isRegistered)
        {
            return ApiResponse<SendOtpResponseDto>.FailureResult(
                $"No registered account found with mobile number +91 {normalizedMobile}. Please switch to Sign Up to create an account.");
        }

        // Only new users can request a signup OTP
        if (isSignupRequest && isRegistered)
        {
            return ApiResponse<SendOtpResponseDto>.FailureResult(
                $"An account is already registered with mobile number +91 {normalizedMobile}. Please switch to Sign In.");
        }

        var purpose = isRegistered ? OtpPurpose.Login : OtpPurpose.Signup;
        var (otpCode, expiresAt) = await _otpService.GenerateAndStoreOtpAsync(normalizedMobile, purpose, ct);

        // Dispatch OTP via Textplate Real SMS Gateway
        var smsResult = await _smsService.SendOtpSmsAsync(normalizedMobile, otpCode, ct);
        if (!smsResult.Success)
        {
            _logger.LogWarning("SMS dispatch failed for mobile {Mobile}: {Error}", normalizedMobile, smsResult.Message);
            return ApiResponse<SendOtpResponseDto>.FailureResult(smsResult.Message);
        }

        var response = new SendOtpResponseDto
        {
            MobileNumber = normalizedMobile,
            ExpiresInSeconds = (int)(expiresAt - DateTimeOffset.UtcNow).TotalSeconds,
            IsRegisteredUser = isRegistered,
            // Per instructions: Once SMS gateway is integrated, do not expose OTP to the web application
            TestOtp = null
        };

        var maskedMobile = normalizedMobile.Length >= 4
            ? $"{normalizedMobile[..2]}******{normalizedMobile[^2..]}"
            : normalizedMobile;

        var message = isRegistered
            ? $"Login OTP sent to {maskedMobile} via SMS."
            : $"Signup OTP sent to {maskedMobile} via SMS.";

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

        var isLogin = request.Purpose?.Equals("login", StringComparison.OrdinalIgnoreCase) == true;
        if (isLogin && user == null)
        {
            return ApiResponse<AuthResponseDto>.FailureResult("No registered account found for this mobile number. Please sign up first.");
        }

        if (user == null)
        {
            // Registration flow
            isNewUser = true;
            var fullName = string.IsNullOrWhiteSpace(request.FullName)
                ? $"User {normalizedMobile[^4..]}"
                : request.FullName.Trim();

            user = new User
            {
                Id = Guid.NewGuid(),
                MobileNumber = normalizedMobile,
                FullName = fullName,
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant(),
                IsMobileVerified = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(user);

            // Seed default profile targets for new user
            var profileTargets = new UserProfileTargets
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                MonthlyIncomeTarget = 10000000, // ₹1,00,000 in Paise
                SavingsRateTarget = 35,
                EmergencyTargetMonths = 6,
                RiskProfile = "Balanced"
            };
            _dbContext.ProfileTargets.Add(profileTargets);

            // Seed default wealth preferences for new user
            var wealthPrefs = new UserWealthPreferences
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Currency = "INR",
                DateFormat = "DD/MM/YYYY",
                WeekStart = "Monday",
                Theme = "light",
                DefaultType = "Expense",
                DefaultPaymentMethod = "UPI"
            };
            _dbContext.WealthPreferences.Add(wealthPrefs);

            // Seed initial categories for new user
            SeedUserDefaultCategories(user.Id);

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
        _dbContext.Categories.AddRange(CategoryDefaults.GetDefaultCategories(userId));
    }
}
