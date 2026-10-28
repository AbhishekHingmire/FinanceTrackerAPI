namespace FinanceTrackerAPI.DTOs.Transactions;

using System.ComponentModel.DataAnnotations;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Expense";
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public Guid AccountId { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public string PaymentMethod { get; set; } = "UPI";
    public string Need { get; set; } = "Need";
    public string Merchant { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public bool Recurring { get; set; }
    public string? LoanId { get; set; }
    public string? InvestmentAction { get; set; }
    public string? InstrumentType { get; set; }
    public decimal? Units { get; set; }
    public decimal? PricePerUnit { get; set; }
}

public class CreateTransactionDto
{
    [Required(ErrorMessage = "Date is required")]
    public string Date { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction type is required")]
    public string Type { get; set; } = "Expense";

    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    public Guid? CategoryId { get; set; }

    [Required(ErrorMessage = "Account is required")]
    public Guid AccountId { get; set; }

    public Guid? DestinationAccountId { get; set; }

    public string PaymentMethod { get; set; } = "UPI";

    public string Need { get; set; } = "Need";

    [MaxLength(120)]
    public string? Merchant { get; set; }

    public List<string>? Tags { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool Recurring { get; set; }

    public string? LoanId { get; set; }
    public string? InvestmentAction { get; set; }
    public string? InstrumentType { get; set; }
    public decimal? Units { get; set; }
    public decimal? PricePerUnit { get; set; }
}

public class UpdateTransactionDto : CreateTransactionDto
{
    public Guid Id { get; set; }
}

public class BulkDeleteTransactionsDto
{
    [Required]
    public List<Guid> Ids { get; set; } = new();
}

public class BulkReassignCategoryDto
{
    [Required]
    public List<Guid> Ids { get; set; } = new();

    [Required]
    public Guid CategoryId { get; set; }
}

public class BulkReassignPriorityDto
{
    [Required]
    public List<Guid> Ids { get; set; } = new();

    [Required]
    public string Priority { get; set; } = "Need";
}
