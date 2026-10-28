namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Holding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public AssetType Type { get; set; }
    public string Broker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    
    // In Paise (BIGINT)
    public long OpeningInvested { get; set; }
    public long CurrentValue { get; set; }
    
    // SIP details
    public bool HasSip { get; set; }
    public long? SipAmount { get; set; } // Paise
    public int? SipDay { get; set; }
    public Guid? SipFromAccountId { get; set; }
    public bool SipActive { get; set; }
    
    // Fixed Deposit details
    public DateOnly? MaturityDate { get; set; }
    public Guid? MaturityAccountId { get; set; }
    public decimal? AnnualRate { get; set; }
    public string? Compounding { get; set; }
    
    public DateOnly? StartDate { get; set; }
    public string? Notes { get; set; }
}
