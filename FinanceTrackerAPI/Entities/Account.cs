namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? Institution { get; set; }
    public string? Identifier { get; set; }
    
    // Amount in Paise (BIGINT)
    public long OpeningBalance { get; set; }
    public DateOnly? OpeningDate { get; set; }
    public bool IsArchived { get; set; }
    public string? Notes { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}
