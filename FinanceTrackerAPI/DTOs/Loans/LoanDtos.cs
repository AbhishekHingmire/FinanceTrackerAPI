namespace FinanceTrackerAPI.DTOs.Loans;

using System.ComponentModel.DataAnnotations;

public class LoanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LoanType { get; set; } = "Home Loan";
    public string LenderInstitution { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal CurrentOutstanding { get; set; }
    public decimal InterestRate { get; set; }
    public decimal EmiAmount { get; set; }
    public Guid RepaymentAccountId { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public int TenureMonths { get; set; }
    public int PaidEmisCount { get; set; }
    public bool AutoRecurringEmi { get; set; } = true;
    public string? Notes { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
}

public class CreateLoanDto
{
    [Required(ErrorMessage = "Loan name is required")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string LoanType { get; set; } = "Home Loan";

    [Required]
    [MaxLength(120)]
    public string LenderInstitution { get; set; } = string.Empty;

    [Range(100, 1000000000)]
    public decimal PrincipalAmount { get; set; }

    public decimal? CurrentOutstanding { get; set; }

    [Range(0.1, 100)]
    public decimal InterestRate { get; set; }

    [Range(1, 100000000)]
    public decimal EmiAmount { get; set; }

    [Required]
    public Guid RepaymentAccountId { get; set; }

    [Required]
    public string StartDate { get; set; } = string.Empty;

    [Range(1, 600)]
    public int TenureMonths { get; set; }

    public int PaidEmisCount { get; set; }

    public bool AutoRecurringEmi { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateLoanDto : CreateLoanDto
{
    public Guid Id { get; set; }
}

public class RecordLoanPaymentDto
{
    [Range(1, 100000000)]
    public decimal Amount { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    public string Date { get; set; } = string.Empty;

    public string Type { get; set; } = "EMI"; // "EMI" or "Prepayment"

    public string? Notes { get; set; }
}
