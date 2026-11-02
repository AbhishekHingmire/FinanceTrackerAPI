namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Recurring;
using FinanceTrackerAPI.DTOs.Transactions;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class RecurringController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<RecurringController> _logger;

    public RecurringController(FinanceTrackerDbContext dbContext, ILogger<RecurringController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<RecurringItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecurring(CancellationToken ct)
    {
        var userId = GetUserId();
        var schedules = await _dbContext.Schedules
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.NextDate)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<RecurringItemDto>>.SuccessResult(schedules.Select(MapToDto).ToList()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RecurringItemDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRecurring([FromBody] CreateRecurringDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var freq = dto.Frequency switch
        {
            "Weekly" => ScheduleFrequency.Weekly,
            "Yearly" => ScheduleFrequency.Yearly,
            _ => ScheduleFrequency.Monthly
        };

        var nextDate = DateOnly.TryParse(dto.NextDate.Split('T')[0], out var nd) ? nd : DateOnly.FromDateTime(DateTime.UtcNow);

        var subtype = dto.Type switch
        {
            "Investment" => EntrySubtype.Sip,
            _ => (!string.IsNullOrWhiteSpace(dto.LoanId) ? EntrySubtype.Emi : EntrySubtype.None)
        };

        // Resolve CategoryId safely
        Guid resolvedCategoryId;
        if (dto.CategoryId.HasValue && dto.CategoryId.Value != Guid.Empty &&
            await _dbContext.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.UserId == userId, ct))
        {
            resolvedCategoryId = dto.CategoryId.Value;
        }
        else
        {
            var cat = await _dbContext.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .FirstOrDefaultAsync(ct);

            if (cat == null)
            {
                var defaults = CategoryDefaults.GetDefaultCategories(userId);
                _dbContext.Categories.AddRange(defaults);
                await _dbContext.SaveChangesAsync(ct);
                cat = defaults.First();
            }
            resolvedCategoryId = cat.Id;
        }

        // Resolve AccountId safely for pure expense tracking mode
        Guid resolvedAccountId;
        if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty &&
            await _dbContext.Accounts.AnyAsync(a => a.Id == dto.AccountId.Value && a.UserId == userId, ct))
        {
            resolvedAccountId = dto.AccountId.Value;
        }
        else
        {
            var acc = await _dbContext.Accounts
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (acc == null)
            {
                acc = new Account
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Name = "General",
                    Type = AccountType.Bank,
                    OpeningBalance = 0,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _dbContext.Accounts.Add(acc);
                await _dbContext.SaveChangesAsync(ct);
            }
            resolvedAccountId = acc.Id;
        }

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Kind = dto.Type == "Income" ? EntryKind.Income : EntryKind.Expense,
            Subtype = subtype,
            Amount = (long)(dto.Amount * 100),
            Frequency = freq,
            NextDate = nextDate,
            AccountId = resolvedAccountId,
            CategoryId = resolvedCategoryId,
            Active = dto.Enabled,
            NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need
        };

        _dbContext.Schedules.Add(schedule);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<RecurringItemDto>.SuccessResult(MapToDto(schedule), "Recurring item created"));
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRecurring(string id, [FromBody] UpdateRecurringDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        Guid? scheduleId = null;
        Guid? loanId = null;

        if (id.StartsWith("rec-loan-", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(id.Substring("rec-loan-".Length), out var plid))
            {
                loanId = plid;
            }
        }
        else if (Guid.TryParse(id, out var parsedGuid))
        {
            scheduleId = parsedGuid;
        }

        Schedule? schedule = null;
        if (scheduleId.HasValue)
        {
            schedule = await _dbContext.Schedules
                .FirstOrDefaultAsync(s => s.Id == scheduleId.Value && s.UserId == userId, ct);
        }

        // If not found by Schedule ID, check if it matches a Loan ID or synthesized loan schedule
        if (schedule == null && (loanId.HasValue || scheduleId.HasValue))
        {
            var targetLoanId = loanId ?? scheduleId!.Value;
            var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == targetLoanId && l.UserId == userId, ct);
            if (loan != null)
            {
                loan.Emi = (long)(dto.Amount * 100);
                loan.AutoPost = dto.Enabled;
                if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty)
                {
                    loan.PayFromAccountId = dto.AccountId.Value;
                }

                schedule = await _dbContext.Schedules
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Subtype == EntrySubtype.Emi && s.Name.Contains(loan.Name), ct);

                if (schedule == null)
                {
                    var nextDate = DateOnly.TryParse(dto.NextDate.Split('T')[0], out var ndVal) ? ndVal : DateOnly.FromDateTime(DateTime.UtcNow);
                    
                    Guid resolvedCatId;
                    if (dto.CategoryId.HasValue && dto.CategoryId.Value != Guid.Empty)
                    {
                        resolvedCatId = dto.CategoryId.Value;
                    }
                    else
                    {
                        var defaultCat = await _dbContext.Categories.Where(c => c.UserId == userId).FirstOrDefaultAsync(ct);
                        resolvedCatId = defaultCat?.Id ?? Guid.NewGuid();
                    }

                    schedule = new Schedule
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Name = string.IsNullOrWhiteSpace(dto.Name) ? $"{loan.Name} (EMI)" : dto.Name.Trim(),
                        Kind = EntryKind.Expense,
                        Subtype = EntrySubtype.Emi,
                        Amount = (long)(dto.Amount * 100),
                        Frequency = ScheduleFrequency.Monthly,
                        NextDate = nextDate,
                        AccountId = loan.PayFromAccountId,
                        CategoryId = resolvedCatId,
                        Active = dto.Enabled,
                        NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need
                    };
                    _dbContext.Schedules.Add(schedule);
                }
            }
        }

        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Recurring schedule not found"));
        }

        schedule.Name = dto.Name.Trim();
        schedule.Kind = dto.Type == "Income" ? EntryKind.Income : EntryKind.Expense;
        schedule.Subtype = dto.Type switch
        {
            "Investment" => EntrySubtype.Sip,
            _ => (!string.IsNullOrWhiteSpace(dto.LoanId) ? EntrySubtype.Emi : schedule.Subtype)
        };
        schedule.Amount = (long)(dto.Amount * 100);
        if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty &&
            await _dbContext.Accounts.AnyAsync(a => a.Id == dto.AccountId.Value && a.UserId == userId, ct))
        {
            schedule.AccountId = dto.AccountId.Value;
        }
        if (dto.CategoryId.HasValue && dto.CategoryId.Value != Guid.Empty)
        {
            schedule.CategoryId = dto.CategoryId.Value;
        }
        schedule.Active = dto.Enabled;
        schedule.NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need;

        if (DateOnly.TryParse(dto.NextDate.Split('T')[0], out var nd))
        {
            schedule.NextDate = nd;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<RecurringItemDto>.SuccessResult(MapToDto(schedule), "Recurring item updated"));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRecurring(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        Guid? scheduleId = null;
        if (id.StartsWith("rec-loan-", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(id.Substring("rec-loan-".Length), out var plid))
            {
                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == plid && l.UserId == userId, ct);
                if (loan != null)
                {
                    loan.AutoPost = false;
                }
            }
        }
        else if (Guid.TryParse(id, out var parsedGuid))
        {
            scheduleId = parsedGuid;
        }

        if (scheduleId.HasValue)
        {
            var schedule = await _dbContext.Schedules
                .FirstOrDefaultAsync(s => s.Id == scheduleId.Value && s.UserId == userId, ct);

            if (schedule != null)
            {
                _dbContext.Schedules.Remove(schedule);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Schedule removed"));
    }

    [HttpPost("{id}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<RecurringItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Toggle(string id, CancellationToken ct)
    {
        var userId = GetUserId();
        Guid? scheduleId = null;
        if (id.StartsWith("rec-loan-", StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(id.Substring("rec-loan-".Length), out var plid))
            {
                var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == plid && l.UserId == userId, ct);
                if (loan != null)
                {
                    loan.AutoPost = !loan.AutoPost;
                    await _dbContext.SaveChangesAsync(ct);
                    return Ok(ApiResponse<object>.SuccessResult(true, $"Loan EMI {(loan.AutoPost ? "activated" : "paused")}"));
                }
            }
        }
        else if (Guid.TryParse(id, out var parsedGuid))
        {
            scheduleId = parsedGuid;
        }

        if (!scheduleId.HasValue)
        {
            return NotFound(ApiResponse<object>.FailureResult("Schedule not found"));
        }

        var schedule = await _dbContext.Schedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId.Value && s.UserId == userId, ct);

        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Schedule not found"));
        }

        schedule.Active = !schedule.Active;
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<RecurringItemDto>.SuccessResult(MapToDto(schedule), $"Schedule {(schedule.Active ? "activated" : "paused")}"));
    }

    [HttpPost("{id:guid}/record-now")]
    [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordNow(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var schedule = await _dbContext.Schedules
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Schedule not found"));
        }

        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            Kind = schedule.Kind,
            Amount = schedule.Amount,
            AccountId = schedule.AccountId,
            CategoryId = schedule.CategoryId,
            NeedWant = schedule.NeedWant ?? NeedWant.Need,
            Source = EntrySource.Schedule,
            SourceId = schedule.Id,
            Note = schedule.Name,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Advance next date
        schedule.NextDate = schedule.Frequency switch
        {
            ScheduleFrequency.Weekly => schedule.NextDate.AddDays(7),
            ScheduleFrequency.Yearly => schedule.NextDate.AddYears(1),
            _ => schedule.NextDate.AddMonths(1)
        };

        _dbContext.Entries.Add(entry);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<TransactionDto>.SuccessResult(new TransactionDto
        {
            Id = entry.Id,
            Date = DateTimeOffset.UtcNow.ToString("o"),
            Description = entry.Note,
            Type = entry.Kind == EntryKind.Income ? "Income" : "Expense",
            Amount = entry.Amount / 100m,
            AccountId = entry.AccountId,
            CategoryId = entry.CategoryId ?? Guid.Empty,
            PaymentMethod = "UPI",
            Need = entry.NeedWant == NeedWant.Want ? "Want" : "Need",
            Recurring = true
        }, "Transaction posted from schedule"));
    }

    private static RecurringItemDto MapToDto(Schedule s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Type = s.Subtype == EntrySubtype.Sip ? "Investment" : (s.Kind == EntryKind.Income ? "Income" : "Expense"),
        Amount = s.Amount / 100m,
        CategoryId = s.CategoryId,
        AccountId = s.AccountId,
        Frequency = s.Frequency switch
        {
            ScheduleFrequency.Weekly => "Weekly",
            ScheduleFrequency.Yearly => "Yearly",
            _ => "Monthly"
        },
        IntervalDays = 30,
        StartDate = s.NextDate.ToString("yyyy-MM-dd"),
        NextDate = s.NextDate.ToString("yyyy-MM-dd"),
        Need = s.NeedWant == NeedWant.Want ? "Want" : "Need",
        Enabled = s.Active
    };
}
