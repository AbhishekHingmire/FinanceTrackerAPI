namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Profile;

public class ProfileController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;

    public ProfileController(FinanceTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.ProfileTargets)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        var p = user.ProfileTargets;
        var dto = new UserProfileDto
        {
            FullName = p?.FullName ?? user.FullName,
            Email = p?.Email ?? user.Email ?? string.Empty,
            Phone = p?.Phone ?? user.MobileNumber,
            Occupation = p?.Occupation ?? "Professional",
            MonthlyIncomeTarget = (p?.MonthlyIncomeTarget ?? 10000000) / 100m,
            EmergencyFundTarget = (p?.EmergencyTargetMonths ?? 6) * ((p?.MonthlyIncomeTarget ?? 10000000) / 100m) * 0.5m,
            SavingsRateTarget = p?.SavingsRateTarget ?? 30,
            RiskProfile = p?.RiskProfile ?? "Balanced",
            Bio = p?.Bio
        };

        return Ok(ApiResponse<UserProfileDto>.SuccessResult(dto));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _dbContext.Users
            .Include(u => u.ProfileTargets)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        if (user.ProfileTargets == null)
        {
            user.ProfileTargets = new Entities.UserProfileTargets
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };
            _dbContext.ProfileTargets.Add(user.ProfileTargets);
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
            user.ProfileTargets.FullName = dto.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email.Trim().ToLowerInvariant();
            user.ProfileTargets.Email = dto.Email.Trim().ToLowerInvariant();
        }

        if (!string.IsNullOrWhiteSpace(dto.Phone)) user.ProfileTargets.Phone = dto.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Occupation)) user.ProfileTargets.Occupation = dto.Occupation.Trim();
        if (dto.MonthlyIncomeTarget.HasValue) user.ProfileTargets.MonthlyIncomeTarget = (long)(dto.MonthlyIncomeTarget.Value * 100);
        if (dto.SavingsRateTarget.HasValue) user.ProfileTargets.SavingsRateTarget = dto.SavingsRateTarget.Value;
        if (!string.IsNullOrWhiteSpace(dto.RiskProfile)) user.ProfileTargets.RiskProfile = dto.RiskProfile.Trim();
        if (dto.Bio != null) user.ProfileTargets.Bio = dto.Bio;

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<UserProfileDto>.SuccessResult(new UserProfileDto
        {
            FullName = user.ProfileTargets.FullName,
            Email = user.ProfileTargets.Email,
            Phone = user.ProfileTargets.Phone ?? user.MobileNumber,
            Occupation = user.ProfileTargets.Occupation ?? "Professional",
            MonthlyIncomeTarget = user.ProfileTargets.MonthlyIncomeTarget / 100m,
            EmergencyFundTarget = user.ProfileTargets.EmergencyTargetMonths * (user.ProfileTargets.MonthlyIncomeTarget / 100m) * 0.5m,
            SavingsRateTarget = user.ProfileTargets.SavingsRateTarget,
            RiskProfile = user.ProfileTargets.RiskProfile,
            Bio = user.ProfileTargets.Bio
        }, "Profile updated successfully"));
    }
}
