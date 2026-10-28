namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public GoalKind Kind { get; set; }
    
    public long Target { get; set; } // Paise
    public DateOnly? TargetDate { get; set; }
    public long MonthlyContribution { get; set; } // Paise
    public string ContributionMode { get; set; } = "manual";
    
    // In Paise
    public long OpeningAmount { get; set; }
    
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Notes { get; set; }
}
