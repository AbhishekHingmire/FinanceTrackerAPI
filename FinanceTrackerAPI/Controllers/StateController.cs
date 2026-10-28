namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Accounts;
using FinanceTrackerAPI.DTOs.Categories;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Goals;
using FinanceTrackerAPI.DTOs.Investments;
using FinanceTrackerAPI.DTOs.Loans;
using FinanceTrackerAPI.DTOs.Preferences;
using FinanceTrackerAPI.DTOs.Profile;
using FinanceTrackerAPI.DTOs.Recurring;
using FinanceTrackerAPI.DTOs.State;
using FinanceTrackerAPI.DTOs.Transactions;
using FinanceTrackerAPI.Enums;

public class StateController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<StateController> _logger;

    public StateController(FinanceTrackerDbContext dbContext, ILogger<StateController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Fetches the entire unified workspace state for the authenticated user.
    /// Eliminates waterfall queries and delivers instant dashboard hydration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceStateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetState(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<object>.FailureResult("Invalid authentication token"));
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.ProfileTargets)
            .Include(u => u.WealthPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        // Fetch Accounts
        var accounts = await _dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        // Fetch Categories
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.UserId == null)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        // Fetch Entries (Transactions)
        var entries = await _dbContext.Entries
            .AsNoTracking()
            .Where(e => e.UserId == userId && !e.IsDeleted)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        // Fetch Loans
        var loans = await _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.StartDate)
            .ToListAsync(ct);

        // Fetch Investments (Holdings)
        var holdings = await _dbContext.Holdings
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.Name)
            .ToListAsync(ct);

        // Fetch Goals
        var goals = await _dbContext.Goals
            .AsNoTracking()
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        // Fetch Recurring Schedules
        var schedules = await _dbContext.Schedules
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.NextDate)
            .ToListAsync(ct);

        // Calculate dynamic balances for accounts
        var accountDtos = accounts.Select(a =>
        {
            var accountInflows = entries
                .Where(e => (e.AccountId == a.Id && e.Kind == EntryKind.Income) || (e.ToRefId == a.Id && e.Kind == EntryKind.Transfer))
                .Sum(e => e.Amount);

            var accountOutflows = entries
                .Where(e => e.AccountId == a.Id && (e.Kind == EntryKind.Expense || e.Kind == EntryKind.Transfer))
                .Sum(e => e.Amount);

            var currentPaise = a.OpeningBalance + accountInflows - accountOutflows;

            return new AccountDto
            {
                Id = a.Id,
                Name = a.Name,
                Kind = a.Type switch
                {
                    AccountType.Cash => "Cash",
                    AccountType.CreditCard => "Credit card",
                    AccountType.Wallet => "Digital wallet",
                    _ => "Bank account"
                },
                Institution = a.Institution,
                Identifier = a.Identifier,
                OpeningBalance = a.OpeningBalance / 100m,
                CurrentBalance = currentPaise / 100m,
                Notes = a.Notes,
                Archived = a.IsArchived
            };
        }).ToList();

        // Map Categories
        var categoryDtos = categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Kind = c.Kind switch
            {
                EntryKind.Income => "Income",
                EntryKind.Transfer => "Transfer",
                _ => "Expense"
            },
            Icon = c.Icon,
            Color = c.Color ?? "blue",
            MonthlyLimit = (c.MonthlyCap ?? 0) / 100m,
            DefaultNeed = c.DefaultNeedWant == NeedWant.Want ? "Want" : "Need",
            Archived = c.IsArchived
        }).ToList();

        // Map Transactions
        var transactionDtos = entries.Select(e =>
        {
            var tags = string.IsNullOrWhiteSpace(e.TagsCsv)
                ? new List<string>()
                : e.TagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var type = e.Subtype switch
            {
                EntrySubtype.Emi => "EMI / Loan Payment",
                EntrySubtype.Borrowed => "Borrow / Loan Taken",
                EntrySubtype.Recovered => "Debt Recovery",
                EntrySubtype.Lent => "Lend / Loan Given",
                _ => e.Kind switch
                {
                    EntryKind.Income => "Income",
                    EntryKind.Transfer => "Transfer",
                    _ => "Expense"
                }
            };

            return new TransactionDto
            {
                Id = e.Id,
                Date = e.Date.ToString("yyyy-MM-dd") + "T" + e.CreatedAt.ToString("HH:mm"),
                Description = e.Note,
                Type = type,
                Amount = e.Amount / 100m,
                CategoryId = e.CategoryId ?? Guid.Empty,
                AccountId = e.AccountId,
                DestinationAccountId = e.ToRefId,
                PaymentMethod = "UPI",
                Need = e.NeedWant == NeedWant.Want ? "Want" : "Need",
                Merchant = e.Merchant ?? e.Note,
                Tags = tags,
                Notes = e.Note,
                Recurring = e.Source == EntrySource.Schedule,
                LoanId = e.SourceId?.ToString()
            };
        }).ToList();

        // Map Loans
        var loanDtos = loans.Select(l => new LoanDto
        {
            Id = l.Id,
            Name = l.Name,
            LoanType = l.Type switch
            {
                LoanType.HomeLoan => "Home Loan",
                LoanType.CarAutoLoan => "Car / Auto Loan",
                LoanType.EducationLoan => "Education Loan",
                LoanType.PersonalLoan => "Personal Loan",
                _ => "Other"
            },
            LenderInstitution = l.Lender,
            PrincipalAmount = l.SanctionedAmount / 100m,
            CurrentOutstanding = (l.OpeningOutstanding ?? l.SanctionedAmount) / 100m,
            InterestRate = l.Rate,
            EmiAmount = l.Emi / 100m,
            RepaymentAccountId = l.PayFromAccountId,
            StartDate = l.StartDate.ToString("yyyy-MM-dd"),
            TenureMonths = l.Tenure,
            PaidEmisCount = l.OpeningInstallmentsPaid ?? 0,
            AutoRecurringEmi = l.AutoPost,
            Notes = l.Notes,
            UpdatedAt = DateTimeOffset.UtcNow.ToString("o")
        }).ToList();

        // Map Investments
        var investmentDtos = holdings.Select(h => new InvestmentHoldingDto
        {
            Id = h.Id,
            Name = h.Name,
            AssetType = h.Type switch
            {
                AssetType.StocksEquity => "Stocks & Equity",
                AssetType.MutualFund => "Mutual fund",
                AssetType.FixedDeposit => "Fixed deposit",
                AssetType.GoldCommodities => "Gold & Commodities",
                AssetType.Crypto => "Crypto",
                _ => "Other"
            },
            InvestedAmount = h.OpeningInvested / 100m,
            CurrentAmount = h.CurrentValue / 100m,
            StartMonthYear = h.StartDate?.ToString("yyyy-MM"),
            Institution = h.Broker,
            Notes = h.Notes,
            UpdatedAt = DateTimeOffset.UtcNow.ToString("o"),
            IsRecurring = h.HasSip,
            SipAmount = (h.SipAmount ?? 0) / 100m,
            SipAccountId = h.SipFromAccountId?.ToString(),
            Status = h.SipActive ? "Active" : "Paused"
        }).ToList();

        // Map Goals
        var goalDtos = goals.Select(g => new GoalDto
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
        }).ToList();

        // Map Recurring
        var recurringDtos = schedules.Select(s => new RecurringItemDto
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
        }).ToList();

        // Profile
        var p = user.ProfileTargets;
        var profileDto = p != null ? new UserProfileDto
        {
            FullName = p.FullName,
            Email = p.Email,
            Phone = p.Phone ?? user.MobileNumber,
            Occupation = p.Occupation ?? "Professional",
            MonthlyIncomeTarget = p.MonthlyIncomeTarget / 100m,
            EmergencyFundTarget = p.EmergencyTargetMonths * (p.MonthlyIncomeTarget / 100m) * 0.5m,
            SavingsRateTarget = p.SavingsRateTarget,
            RiskProfile = p.RiskProfile,
            Bio = p.Bio
        } : new UserProfileDto
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Phone = user.MobileNumber
        };

        // Preferences
        var w = user.WealthPreferences;
        var prefsDto = w != null ? new WealthPreferencesDto
        {
            DisplayName = user.FullName,
            Currency = w.Currency == "USD" ? "USD" : "INR",
            DateFormat = "DD/MM/YYYY",
            Language = "English",
            WeekStart = w.WeekStart == "Sunday" ? "Sunday" : "Monday",
            Theme = w.Theme,
            DefaultType = w.DefaultType == "Income" ? "Income" : "Expense",
            DefaultAccount = w.DefaultAccountId?.ToString() ?? (accounts.FirstOrDefault()?.Id.ToString() ?? string.Empty),
            DefaultPaymentMethod = w.DefaultPaymentMethod,
            RecurringReminders = w.AlertEmiSip3Days,
            BudgetReminders = w.AlertBudget90Percent
        } : new WealthPreferencesDto
        {
            DisplayName = user.FullName
        };

        // If the user has created at least one account, mark onboarded as true
        var isOnboarded = accounts.Any();

        var state = new WorkspaceStateDto
        {
            Session = true,
            Onboarded = isOnboarded,
            Preferences = prefsDto,
            Profile = profileDto,
            Accounts = accountDtos,
            Categories = categoryDtos,
            Transactions = transactionDtos,
            Loans = loanDtos,
            Investments = investmentDtos,
            Funds = goalDtos,
            Recurring = recurringDtos
        };

        return Ok(ApiResponse<WorkspaceStateDto>.SuccessResult(state));
    }

    /// <summary>
    /// Updates workspace state preferences and profile in a single atomic payload.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveState([FromBody] SaveWorkspaceStateDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty)
        {
            return Unauthorized(ApiResponse<object>.FailureResult("Unauthorized"));
        }

        var user = await _dbContext.Users
            .Include(u => u.ProfileTargets)
            .Include(u => u.WealthPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("User not found"));
        }

        if (dto.Profile != null && user.ProfileTargets != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Profile.FullName)) user.ProfileTargets.FullName = dto.Profile.FullName.Trim();
            if (dto.Profile.MonthlyIncomeTarget.HasValue) user.ProfileTargets.MonthlyIncomeTarget = (long)(dto.Profile.MonthlyIncomeTarget.Value * 100);
            if (dto.Profile.SavingsRateTarget.HasValue) user.ProfileTargets.SavingsRateTarget = dto.Profile.SavingsRateTarget.Value;
            if (!string.IsNullOrWhiteSpace(dto.Profile.RiskProfile)) user.ProfileTargets.RiskProfile = dto.Profile.RiskProfile;
        }

        if (dto.Preferences != null && user.WealthPreferences != null)
        {
            if (!string.IsNullOrWhiteSpace(dto.Preferences.Currency)) user.WealthPreferences.Currency = dto.Preferences.Currency;
            if (!string.IsNullOrWhiteSpace(dto.Preferences.Theme)) user.WealthPreferences.Theme = dto.Preferences.Theme;
            if (!string.IsNullOrWhiteSpace(dto.Preferences.DefaultPaymentMethod)) user.WealthPreferences.DefaultPaymentMethod = dto.Preferences.DefaultPaymentMethod;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "State updated successfully"));
    }
}
