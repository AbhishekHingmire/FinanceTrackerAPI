namespace FinanceTrackerAPI.Data;

using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Entities;

public class FinanceTrackerDbContext : DbContext
{
    public FinanceTrackerDbContext(DbContextOptions<FinanceTrackerDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Holding> Holdings => Set<Holding>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<UserProfileTargets> ProfileTargets => Set<UserProfileTargets>();
    public DbSet<UserWealthPreferences> WealthPreferences => Set<UserWealthPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Property(u => u.MobileNumber).HasMaxLength(20).IsRequired();
            b.HasIndex(u => u.MobileNumber).IsUnique();
            b.Property(u => u.FullName).HasMaxLength(120).IsRequired();
            b.Property(u => u.Email).HasMaxLength(150);
            b.Property(u => u.RowVersion).IsRowVersion();
            b.HasIndex(u => u.Email).HasFilter("[Email] IS NOT NULL");
        });

        // UserOtp
        modelBuilder.Entity<UserOtp>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.MobileNumber).HasMaxLength(20).IsRequired();
            b.Property(o => o.OtpHash).HasMaxLength(128).IsRequired();
            b.HasIndex(o => new { o.MobileNumber, o.IsUsed, o.ExpiresAt });
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasKey(r => r.Id);
            b.Property(r => r.Token).HasMaxLength(256).IsRequired();
            b.HasIndex(r => r.Token).IsUnique();
            b.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Account
        modelBuilder.Entity<Account>(b =>
        {
            b.HasKey(a => a.Id);
            b.Property(a => a.Name).HasMaxLength(120).IsRequired();
            b.Property(a => a.Institution).HasMaxLength(100);
            b.Property(a => a.Identifier).HasMaxLength(50);
            b.Property(a => a.RowVersion).IsRowVersion();
            b.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(a => new { a.UserId, a.IsArchived });
        });

        // Category
        modelBuilder.Entity<Category>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(100).IsRequired();
            b.Property(c => c.Icon).HasMaxLength(30);
            b.Property(c => c.Color).HasMaxLength(30);
            b.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(c => new { c.UserId, c.Kind, c.IsArchived });
        });

        // Entry
        modelBuilder.Entity<Entry>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Note).HasMaxLength(500);
            b.Property(e => e.Merchant).HasMaxLength(150);
            b.Property(e => e.ToRefName).HasMaxLength(150);
            b.Property(e => e.TagsCsv).HasMaxLength(250);

            b.HasOne(e => e.User)
                .WithMany(u => u.Entries)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Account)
                .WithMany(a => a.Entries)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Category)
                .WithMany(c => c.Entries)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(e => new { e.UserId, e.Date, e.IsDeleted })
                .IncludeProperties(e => new { e.Kind, e.Subtype, e.Amount, e.AccountId, e.CategoryId, e.NeedWant });

            b.HasIndex(e => new { e.SourceId, e.Date, e.Status });

            b.HasQueryFilter(e => !e.IsDeleted);
        });

        // Loan
        modelBuilder.Entity<Loan>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Name).HasMaxLength(120).IsRequired();
            b.Property(l => l.Lender).HasMaxLength(120).IsRequired();
            b.Property(l => l.Rate).HasPrecision(6, 4);

            b.HasOne(l => l.User)
                .WithMany(u => u.Loans)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(l => l.PayFromAccount)
                .WithMany()
                .HasForeignKey(l => l.PayFromAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(l => l.UserId);
        });

        // Holding
        modelBuilder.Entity<Holding>(b =>
        {
            b.HasKey(h => h.Id);
            b.Property(h => h.Name).HasMaxLength(150).IsRequired();
            b.Property(h => h.Broker).HasMaxLength(100);
            b.Property(h => h.AnnualRate).HasPrecision(6, 4);

            b.HasOne(h => h.User)
                .WithMany(u => u.Holdings)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(h => new { h.UserId, h.Type });
        });

        // Goal
        modelBuilder.Entity<Goal>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.Name).HasMaxLength(120).IsRequired();
            b.Property(g => g.ContributionMode).HasMaxLength(20);
            b.Property(g => g.Icon).HasMaxLength(30);
            b.Property(g => g.Color).HasMaxLength(30);

            b.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(g => new { g.UserId, g.Kind });
        });

        // Schedule
        modelBuilder.Entity<Schedule>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(120).IsRequired();

            b.HasOne(s => s.User)
                .WithMany(u => u.Schedules)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Account)
                .WithMany()
                .HasForeignKey(s => s.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.Category)
                .WithMany()
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(s => new { s.UserId, s.Active, s.NextDate });
        });

        // UserProfileTargets
        modelBuilder.Entity<UserProfileTargets>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.FullName).HasMaxLength(120).IsRequired();
            b.Property(p => p.Email).HasMaxLength(150);
            b.Property(p => p.RiskProfile).HasMaxLength(30);

            b.HasOne(p => p.User)
                .WithOne(u => u.ProfileTargets)
                .HasForeignKey<UserProfileTargets>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserWealthPreferences
        modelBuilder.Entity<UserWealthPreferences>(b =>
        {
            b.HasKey(w => w.Id);
            b.Property(w => w.Currency).HasMaxLength(10);
            b.Property(w => w.DateFormat).HasMaxLength(20);
            b.Property(w => w.Theme).HasMaxLength(20);

            b.HasOne(w => w.User)
                .WithOne(u => u.WealthPreferences)
                .HasForeignKey<UserWealthPreferences>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
