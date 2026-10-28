namespace FinanceTrackerAPI.DTOs.Goals;

using System.ComponentModel.DataAnnotations;

public class GoalDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Custom";
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal? MonthlyContribution { get; set; }
    public string? TargetDate { get; set; }
    public Guid? LinkedAccountId { get; set; }
    public string? Notes { get; set; }
    public string? Icon { get; set; } = "🎯";
    public string? Color { get; set; } = "emerald";
    public string Status { get; set; } = "Active";
    public string UpdatedAt { get; set; } = string.Empty;
}

public class CreateGoalDto
{
    [Required(ErrorMessage = "Goal name is required")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "Custom";

    [Range(1, 1000000000)]
    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }
    public decimal? MonthlyContribution { get; set; }
    public string? TargetDate { get; set; }
    public Guid? LinkedAccountId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
    public string? Icon { get; set; } = "🎯";
    public string? Color { get; set; } = "emerald";
    public string Status { get; set; } = "Active";
}

public class UpdateGoalDto : CreateGoalDto
{
    public Guid Id { get; set; }
}

public class ContributeToGoalDto
{
    [Range(1, 100000000)]
    public decimal Amount { get; set; }

    public Guid? AccountId { get; set; }
    public string? Notes { get; set; }
}
