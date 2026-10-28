namespace FinanceTrackerAPI.DTOs.Recurring;

using System.ComponentModel.DataAnnotations;

public class RecurringItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public int IntervalDays { get; set; } = 30;
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }
    public string NextDate { get; set; } = string.Empty;
    public string? Need { get; set; } = "Need";
    public bool Enabled { get; set; } = true;
    public string? LoanId { get; set; }
    public string? InvestmentId { get; set; }
}

public class CreateRecurringDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "Expense";

    [Range(0.01, 1000000000)]
    public decimal Amount { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    public string Frequency { get; set; } = "Monthly";
    public int IntervalDays { get; set; } = 30;

    [Required]
    public string StartDate { get; set; } = string.Empty;
    public string? EndDate { get; set; }

    [Required]
    public string NextDate { get; set; } = string.Empty;

    public string? Need { get; set; } = "Need";
    public bool Enabled { get; set; } = true;

    public string? LoanId { get; set; }
    public string? InvestmentId { get; set; }
}

public class UpdateRecurringDto : CreateRecurringDto
{
    public Guid Id { get; set; }
}
