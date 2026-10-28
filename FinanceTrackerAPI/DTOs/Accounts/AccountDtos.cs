namespace FinanceTrackerAPI.DTOs.Accounts;

using System.ComponentModel.DataAnnotations;

public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "Bank account";
    public string? Institution { get; set; }
    public string? Identifier { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Notes { get; set; }
    public bool Archived { get; set; }
}

public class CreateAccountDto
{
    [Required(ErrorMessage = "Account name is required")]
    [MaxLength(120, ErrorMessage = "Account name cannot exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account kind is required")]
    public string Kind { get; set; } = "Bank account";

    [MaxLength(120)]
    public string? Institution { get; set; }

    [MaxLength(50)]
    public string? Identifier { get; set; }

    public decimal OpeningBalance { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class UpdateAccountDto
{
    [Required(ErrorMessage = "Account name is required")]
    [MaxLength(120, ErrorMessage = "Account name cannot exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    public string Kind { get; set; } = "Bank account";

    [MaxLength(120)]
    public string? Institution { get; set; }

    [MaxLength(50)]
    public string? Identifier { get; set; }

    public decimal OpeningBalance { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool Archived { get; set; }
}

public class ReconcileAccountDto
{
    [Required(ErrorMessage = "New balance is required")]
    public decimal NewBalance { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
