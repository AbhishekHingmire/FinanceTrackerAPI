namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateOnly Date { get; set; }
    public EntryStatus Status { get; set; } = EntryStatus.Posted;
    public EntryKind Kind { get; set; }
    
    // Amount in Paise (BIGINT)
    public long Amount { get; set; }
    
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public ToRefKind? ToRefKind { get; set; }
    public Guid? ToRefId { get; set; }
    public string? ToRefName { get; set; }
    
    public NeedWant NeedWant { get; set; } = NeedWant.Need;
    public EntrySubtype Subtype { get; set; } = EntrySubtype.None;
    public EntrySource Source { get; set; } = EntrySource.Manual;
    public Guid? SourceId { get; set; }
    
    public int? InstallmentNo { get; set; }
    public long? PrincipalPart { get; set; }
    public long? InterestPart { get; set; }
    
    public Guid? TransferId { get; set; }
    
    public string Note { get; set; } = string.Empty;
    public string? Merchant { get; set; }
    public string? TagsCsv { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
