namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Lender { get; set; } = string.Empty;
    public LoanType Type { get; set; }
    
    // Sanctioned Loan Principal in Paise (BIGINT)
    public long SanctionedAmount { get; set; }
    public decimal Rate { get; set; } // Annual % e.g. 8.4000
    public int Tenure { get; set; } // Total months e.g. 180
    
    public DateOnly StartDate { get; set; }
    public long Emi { get; set; } // Paise
    public int EmiDay { get; set; } // Day of month (1-31)
    
    public Guid PayFromAccountId { get; set; }
    public Account PayFromAccount { get; set; } = null!;
    public bool AutoPost { get; set; }
    
    public LoanMode Mode { get; set; } = LoanMode.New;
    public long? OpeningOutstanding { get; set; } // Paise
    public int? OpeningInstallmentsPaid { get; set; }
    public string? Notes { get; set; }
}
