namespace FinanceTrackerAPI.Services.Interfaces;

using System.Security.Claims;
using FinanceTrackerAPI.Entities;

public interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
