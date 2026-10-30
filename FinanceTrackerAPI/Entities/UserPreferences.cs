namespace FinanceTrackerAPI.Entities;

public class UserProfileTargets
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Occupation { get; set; }
    public string? Bio { get; set; }
    
    public long MonthlyIncomeTarget { get; set; } // Paise
    public int SavingsRateTarget { get; set; } = 35; // %
    public int EmergencyTargetMonths { get; set; } = 6;
    public string RiskProfile { get; set; } = "Balanced";
}

public class UserWealthPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Currency { get; set; } = "INR";
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string WeekStart { get; set; } = "Monday";
    public string Theme { get; set; } = "light";
    public Guid? DefaultAccountId { get; set; }
    public string DefaultType { get; set; } = "Expense";
    public string DefaultPaymentMethod { get; set; } = "UPI";
    public int? SalaryDay { get; set; }
    
    public bool AlertEmiSip3Days { get; set; } = true;
    public bool AlertBalanceShortWarning { get; set; } = true;
    public bool AlertBudget90Percent { get; set; } = true;
    public bool AlertGoalBehindSchedule { get; set; } = true;
    public bool AlertGoalsExceedCash { get; set; } = true;
}
