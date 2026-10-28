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

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Kind = dto.Type == "Income" ? EntryKind.Income : EntryKind.Expense,
            Amount = (long)(dto.Amount * 100),
            Frequency = freq,
            NextDate = nextDate,
            AccountId = dto.AccountId,
            CategoryId = dto.CategoryId,
            Active = dto.Enabled,
            NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need
        };

        _dbContext.Schedules.Add(schedule);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<RecurringItemDto>.SuccessResult(MapToDto(schedule), "Recurring item created"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RecurringItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRecurring(Guid id, [FromBody] UpdateRecurringDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var schedule = await _dbContext.Schedules
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Recurring schedule not found"));
        }

        schedule.Name = dto.Name.Trim();
        schedule.Kind = dto.Type == "Income" ? EntryKind.Income : EntryKind.Expense;
        schedule.Amount = (long)(dto.Amount * 100);
        schedule.AccountId = dto.AccountId;
        schedule.CategoryId = dto.CategoryId;
        schedule.Active = dto.Enabled;
        schedule.NeedWant = dto.Need == "Want" ? NeedWant.Want : NeedWant.Need;

        if (DateOnly.TryParse(dto.NextDate.Split('T')[0], out var nd))
        {
            schedule.NextDate = nd;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<RecurringItemDto>.SuccessResult(MapToDto(schedule), "Recurring item updated"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRecurring(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var schedule = await _dbContext.Schedules
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (schedule == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Schedule not found"));
        }

        _dbContext.Schedules.Remove(schedule);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Schedule removed"));
    }

    [HttpPost("{id:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<RecurringItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var schedule = await _dbContext.Schedules
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

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
        Type = s.Kind == EntryKind.Income ? "Income" : "Expense",
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
