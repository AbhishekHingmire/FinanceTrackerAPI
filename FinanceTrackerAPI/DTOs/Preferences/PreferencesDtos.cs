namespace FinanceTrackerAPI.DTOs.Preferences;

public class WealthPreferencesDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Currency { get; set; } = "INR";
    public string DateFormat { get; set; } = "DD/MM/YYYY";
    public string Language { get; set; } = "English";
    public string WeekStart { get; set; } = "Monday";
    public string Theme { get; set; } = "light";
    public int? SalaryDay { get; set; } = 1;
    public string DefaultType { get; set; } = "Expense";
    public string DefaultAccount { get; set; } = string.Empty;
    public string DefaultPaymentMethod { get; set; } = "UPI";
    public bool ConfirmTransactions { get; set; } = false;
    public bool RecurringReminders { get; set; } = true;
    public bool BudgetReminders { get; set; } = true;
    public bool OtherReminders { get; set; } = true;
}

public class UpdatePreferencesDto
{
    public string? DisplayName { get; set; }
    public string? Currency { get; set; }
    public string? DateFormat { get; set; }
    public string? Language { get; set; }
    public string? WeekStart { get; set; }
    public string? Theme { get; set; }
    public int? SalaryDay { get; set; }
    public string? DefaultType { get; set; }
    public string? DefaultAccount { get; set; }
    public string? DefaultPaymentMethod { get; set; }
    public bool? ConfirmTransactions { get; set; }
    public bool? RecurringReminders { get; set; }
    public bool? BudgetReminders { get; set; }
    public bool? OtherReminders { get; set; }
}
