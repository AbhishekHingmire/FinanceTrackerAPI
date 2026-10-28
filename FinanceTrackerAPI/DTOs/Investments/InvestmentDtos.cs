namespace FinanceTrackerAPI.DTOs.Investments;

using System.ComponentModel.DataAnnotations;

public class InvestmentHoldingDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = "Mutual fund";
    public decimal InvestedAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public string? StartMonthYear { get; set; }
    public string? EndMonthYear { get; set; }
    public string? Institution { get; set; }
    public string? Notes { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string? RecurringId { get; set; }
    public decimal? SipAmount { get; set; }
    public string? SipFrequency { get; set; }
    public string? SipAccountId { get; set; }
    public int? SipDay { get; set; }
    public string? InvestmentStyle { get; set; }
    public string? Status { get; set; } = "Active";
}

public class CreateInvestmentDto
{
    [Required(ErrorMessage = "Investment name is required")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string AssetType { get; set; } = "Mutual fund";

    [Range(0, 1000000000)]
    public decimal InvestedAmount { get; set; }

    [Range(0, 1000000000)]
    public decimal CurrentAmount { get; set; }

    public string? StartMonthYear { get; set; }
    public string? EndMonthYear { get; set; }

    [MaxLength(120)]
    public string? Institution { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public bool IsRecurring { get; set; }
    public decimal? SipAmount { get; set; }
    public string? SipFrequency { get; set; }
    public Guid? SipAccountId { get; set; }
    public int? SipDay { get; set; }
    public string? InvestmentStyle { get; set; }
    public string? Status { get; set; } = "Active";
}

public class UpdateInvestmentDto : CreateInvestmentDto
{
    public Guid Id { get; set; }
}
