namespace FinanceTrackerAPI.DTOs.State;

using FinanceTrackerAPI.DTOs.Accounts;
using FinanceTrackerAPI.DTOs.Categories;
using FinanceTrackerAPI.DTOs.Goals;
using FinanceTrackerAPI.DTOs.Investments;
using FinanceTrackerAPI.DTOs.Loans;
using FinanceTrackerAPI.DTOs.Preferences;
using FinanceTrackerAPI.DTOs.Profile;
using FinanceTrackerAPI.DTOs.Recurring;
using FinanceTrackerAPI.DTOs.Transactions;

public class WorkspaceStateDto
{
    public bool Session { get; set; } = true;
    public bool Onboarded { get; set; } = false;
    public WealthPreferencesDto Preferences { get; set; } = new();
    public UserProfileDto? Profile { get; set; }
    public List<AccountDto> Accounts { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
    public List<RecurringItemDto> Recurring { get; set; } = new();
    public List<InvestmentHoldingDto> Investments { get; set; } = new();
    public List<LoanDto> Loans { get; set; } = new();
    public List<GoalDto> Funds { get; set; } = new();
    public List<object> Plans { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<object> CustomFields { get; set; } = new();
    public List<object> Notifications { get; set; } = new();
}

public class SaveWorkspaceStateDto
{
    public bool? Onboarded { get; set; }
    public UpdatePreferencesDto? Preferences { get; set; }
    public UpdateProfileDto? Profile { get; set; }
    public List<CreateAccountDto>? Accounts { get; set; }
    public List<CreateCategoryDto>? Categories { get; set; }
    public List<CreateTransactionDto>? Transactions { get; set; }
}
