namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; } // Null for system presets
    public User? User { get; set; }
    public string Name { get; set; } = string.Empty;
    public EntryKind Kind { get; set; }
    public NeedWant? DefaultNeedWant { get; set; }
    
    // Monthly budget limit in Paise (BIGINT)
    public long? MonthlyCap { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string? Color { get; set; }
    public bool IsArchived { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}
