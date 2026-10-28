namespace FinanceTrackerAPI.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MobileNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsMobileVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Optimistic Concurrency Token
    public byte[] RowVersion { get; set; } = [];

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Holding> Holdings { get; set; } = new List<Holding>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public UserProfileTargets? ProfileTargets { get; set; }
    public UserWealthPreferences? WealthPreferences { get; set; }
}
