namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Goals;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

public class GoalsController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<GoalsController> _logger;

    public GoalsController(FinanceTrackerDbContext dbContext, ILogger<GoalsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GoalDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGoals(CancellationToken ct)
    {
        var userId = GetUserId();
        var goals = await _dbContext.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<GoalDto>>.SuccessResult(goals.Select(MapToDto).ToList()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GoalDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGoal([FromBody] CreateGoalDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var kind = dto.Type switch
        {
            "Emergency" => GoalKind.Emergency,
            "PurchaseGoal" => GoalKind.Sinking,
            _ => GoalKind.Goal
        };

        DateOnly? targetDate = null;
        if (!string.IsNullOrWhiteSpace(dto.TargetDate) && DateOnly.TryParse(dto.TargetDate.Split('T')[0], out var td))
        {
            targetDate = td;
        }

        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Kind = kind,
            Target = (long)(dto.TargetAmount * 100),
            OpeningAmount = (long)(dto.CurrentAmount * 100),
            MonthlyContribution = dto.MonthlyContribution.HasValue ? (long)(dto.MonthlyContribution.Value * 100) : 0,
            TargetDate = targetDate,
            Icon = dto.Icon ?? "🎯",
            Color = dto.Color ?? "emerald",
            Notes = dto.Notes
        };

        _dbContext.Goals.Add(goal);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<GoalDto>.SuccessResult(MapToDto(goal), "Goal created successfully"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateGoal(Guid id, [FromBody] UpdateGoalDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var goal = await _dbContext.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);

        if (goal == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Goal not found"));
        }

        goal.Name = dto.Name.Trim();
        goal.Target = (long)(dto.TargetAmount * 100);
        goal.OpeningAmount = (long)(dto.CurrentAmount * 100);
        if (dto.MonthlyContribution.HasValue)
        {
            goal.MonthlyContribution = (long)(dto.MonthlyContribution.Value * 100);
        }
        if (!string.IsNullOrWhiteSpace(dto.TargetDate) && DateOnly.TryParse(dto.TargetDate.Split('T')[0], out var td))
        {
            goal.TargetDate = td;
        }
        goal.Icon = dto.Icon ?? "🎯";
        goal.Color = dto.Color ?? "emerald";
        goal.Notes = dto.Notes;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<GoalDto>.SuccessResult(MapToDto(goal), "Goal updated"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteGoal(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var goal = await _dbContext.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);

        if (goal == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Goal not found"));
        }

        _dbContext.Goals.Remove(goal);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Goal deleted"));
    }

    [HttpPost("{id:guid}/contribute")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Contribute(Guid id, [FromBody] ContributeToGoalDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var goal = await _dbContext.Goals
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId, ct);

        if (goal == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Goal not found"));
        }

        var paise = (long)(dto.Amount * 100);
        goal.OpeningAmount += paise;

        if (dto.AccountId.HasValue && dto.AccountId.Value != Guid.Empty)
        {
            // Record out-flow from account towards goal
            var entry = new Entry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Kind = EntryKind.Transfer,
                Amount = paise,
                AccountId = dto.AccountId.Value,
                Note = string.IsNullOrWhiteSpace(dto.Notes) ? $"Contribution to {goal.Name}" : dto.Notes.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Entries.Add(entry);
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Contribution recorded"));
    }

    private static GoalDto MapToDto(Goal g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        Type = g.Kind switch
        {
            GoalKind.Emergency => "Emergency",
            GoalKind.Sinking => "PurchaseGoal",
            _ => "Custom"
        },
        TargetAmount = g.Target / 100m,
        CurrentAmount = g.OpeningAmount / 100m,
        MonthlyContribution = g.MonthlyContribution / 100m,
        TargetDate = g.TargetDate?.ToString("yyyy-MM-dd"),
        Icon = g.Icon ?? "🎯",
        Color = g.Color ?? "emerald",
        Notes = g.Notes,
        Status = "Active",
        UpdatedAt = DateTimeOffset.UtcNow.ToString("o")
    };
}
