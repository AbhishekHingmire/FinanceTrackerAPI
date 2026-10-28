namespace FinanceTrackerAPI.Entities;

using FinanceTrackerAPI.Enums;

public class Schedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public EntryKind Kind { get; set; }
    public long Amount { get; set; } // Paise
    public ScheduleFrequency Frequency { get; set; }
    public DateOnly NextDate { get; set; }
    
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public bool AutoPost { get; set; }
    public bool Active { get; set; } = true;
    public NeedWant? NeedWant { get; set; }
    public EntrySubtype? Subtype { get; set; }
}
