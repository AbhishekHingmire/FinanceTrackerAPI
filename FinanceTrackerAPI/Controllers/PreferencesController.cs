namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Preferences;

public class PreferencesController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;

    public PreferencesController(FinanceTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WealthPreferencesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.WealthPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        var w = user.WealthPreferences;
        var dto = new WealthPreferencesDto
        {
            DisplayName = user.FullName,
            Currency = w?.Currency ?? "INR",
            DateFormat = w?.DateFormat ?? "DD/MM/YYYY",
            Language = "English",
            WeekStart = w?.WeekStart ?? "Monday",
            Theme = w?.Theme ?? "light",
            DefaultType = w?.DefaultType ?? "Expense",
            DefaultAccount = w?.DefaultAccountId?.ToString() ?? string.Empty,
            DefaultPaymentMethod = w?.DefaultPaymentMethod ?? "UPI",
            SalaryDay = w?.SalaryDay ?? 1,
            RecurringReminders = w?.AlertEmiSip3Days ?? true,
            BudgetReminders = w?.AlertBudget90Percent ?? true
        };

        return Ok(ApiResponse<WealthPreferencesDto>.SuccessResult(dto));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<WealthPreferencesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await _dbContext.Users
            .Include(u => u.WealthPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        if (user.WealthPreferences == null)
        {
            user.WealthPreferences = new Entities.UserWealthPreferences
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            _dbContext.WealthPreferences.Add(user.WealthPreferences);
        }

        if (!string.IsNullOrWhiteSpace(dto.Currency)) user.WealthPreferences.Currency = dto.Currency.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DateFormat)) user.WealthPreferences.DateFormat = dto.DateFormat.Trim();
        if (!string.IsNullOrWhiteSpace(dto.WeekStart)) user.WealthPreferences.WeekStart = dto.WeekStart.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Theme)) user.WealthPreferences.Theme = dto.Theme.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DefaultType)) user.WealthPreferences.DefaultType = dto.DefaultType.Trim();
        if (!string.IsNullOrWhiteSpace(dto.DefaultPaymentMethod)) user.WealthPreferences.DefaultPaymentMethod = dto.DefaultPaymentMethod.Trim();
        if (dto.SalaryDay.HasValue && dto.SalaryDay.Value >= 1 && dto.SalaryDay.Value <= 31) user.WealthPreferences.SalaryDay = dto.SalaryDay.Value;
        if (dto.RecurringReminders.HasValue) user.WealthPreferences.AlertEmiSip3Days = dto.RecurringReminders.Value;
        if (dto.BudgetReminders.HasValue) user.WealthPreferences.AlertBudget90Percent = dto.BudgetReminders.Value;

        if (!string.IsNullOrWhiteSpace(dto.DefaultAccount) && Guid.TryParse(dto.DefaultAccount, out var accId))
        {
            user.WealthPreferences.DefaultAccountId = accId;
        }

        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<WealthPreferencesDto>.SuccessResult(new WealthPreferencesDto
        {
            DisplayName = user.FullName,
            Currency = user.WealthPreferences.Currency,
            DateFormat = user.WealthPreferences.DateFormat,
            Language = "English",
            WeekStart = user.WealthPreferences.WeekStart,
            Theme = user.WealthPreferences.Theme,
            DefaultType = user.WealthPreferences.DefaultType,
            DefaultAccount = user.WealthPreferences.DefaultAccountId?.ToString() ?? string.Empty,
            DefaultPaymentMethod = user.WealthPreferences.DefaultPaymentMethod,
            SalaryDay = user.WealthPreferences.SalaryDay ?? 1,
            RecurringReminders = user.WealthPreferences.AlertEmiSip3Days,
            BudgetReminders = user.WealthPreferences.AlertBudget90Percent
        }, "Preferences updated"));
    }
}
