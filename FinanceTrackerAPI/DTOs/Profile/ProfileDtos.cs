namespace FinanceTrackerAPI.DTOs.Profile;

using System.ComponentModel.DataAnnotations;

public class UserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Occupation { get; set; } = "Professional";
    public decimal MonthlyIncomeTarget { get; set; }
    public decimal EmergencyFundTarget { get; set; }
    public int SavingsRateTarget { get; set; } = 30;
    public string RiskProfile { get; set; } = "Balanced";
    public string? Bio { get; set; }
}

public class UpdateProfileDto
{
    [MaxLength(120)]
    public string? FullName { get; set; }

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Occupation { get; set; }

    public decimal? MonthlyIncomeTarget { get; set; }
    public decimal? EmergencyFundTarget { get; set; }
    public int? SavingsRateTarget { get; set; }
    public string? RiskProfile { get; set; }
    public string? Bio { get; set; }
}
