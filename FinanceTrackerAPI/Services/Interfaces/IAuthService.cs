namespace FinanceTrackerAPI.Services.Interfaces;

using FinanceTrackerAPI.DTOs.Auth;
using FinanceTrackerAPI.DTOs.Common;

public interface IAuthService
{
    Task<ApiResponse<SendOtpResponseDto>> SendOtpAsync(SendOtpRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponseDto>> VerifyOtpAndAuthenticateAsync(VerifyOtpRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);
    Task<ApiResponse<bool>> LogoutAsync(Guid userId, string? refreshToken, CancellationToken ct = default);
    Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
