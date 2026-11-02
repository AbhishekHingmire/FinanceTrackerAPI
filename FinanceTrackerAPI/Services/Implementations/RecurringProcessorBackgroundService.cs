namespace FinanceTrackerAPI.Services.Implementations;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;

/// <summary>
/// Background service hosted within the ASP.NET Core API process (no separate deployment).
/// Continuously monitors and executes due recurring schedules, performs background deduction/entry postings,
/// verifies category budget thresholds, and issues unified in-app notifications.
/// </summary>
public class RecurringProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringProcessorBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public RecurringProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringProcessorBackgroundService started.");

        // Initial small delay to let database migrations and startup initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueSchedulesAndBudgetsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred in RecurringProcessorBackgroundService execution loop.");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("RecurringProcessorBackgroundService stopped.");
    }

    private async Task ProcessDueSchedulesAndBudgetsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceTrackerDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var endOfMonth = new DateOnly(today.Year, today.Month, daysInMonth);

        // 1. Process Due Recurring Schedules
        var dueSchedules = await db.Schedules
            .Where(s => s.Active && s.NextDate <= today)
            .OrderBy(s => s.NextDate)
            .ToListAsync(ct);

        foreach (var schedule in dueSchedules)
        {
            // Auto-post entry
            var entry = new Entry
            {
                Id = Guid.NewGuid(),
                UserId = schedule.UserId,
                Date = schedule.NextDate,
                Kind = schedule.Kind,
                Subtype = schedule.Subtype ?? EntrySubtype.None,
                Amount = schedule.Amount,
                AccountId = schedule.AccountId,
                CategoryId = schedule.CategoryId,
                NeedWant = schedule.NeedWant ?? NeedWant.Need,
                Note = $"{schedule.Name} (Auto-posted)",
                Merchant = schedule.Name,
                Source = EntrySource.Schedule,
                SourceId = schedule.Id,
                CreatedAt = now
            };

            db.Entries.Add(entry);

            // Create notification for the user
            var formattedAmount = schedule.Amount / 100m;
            var notif = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = schedule.UserId,
                Title = "Recurring Payment Processed",
                Message = $"{schedule.Name} of ₹{formattedAmount:N0} was auto-recorded for {schedule.NextDate:dd MMM yyyy}.",
                Type = "recurring",
                Link = "/transactions",
                IsRead = false,
                CreatedAt = now
            };
            db.Notifications.Add(notif);

            // Advance NextDate
            var nextDate = schedule.Frequency switch
            {
                ScheduleFrequency.Weekly => schedule.NextDate.AddDays(7),
                ScheduleFrequency.Yearly => schedule.NextDate.AddYears(1),
                _ => schedule.NextDate.AddMonths(1)
            };
            schedule.NextDate = nextDate;

            _logger.LogInformation("Auto-posted recurring schedule {ScheduleId} ({Name}) for user {UserId}. Next date: {NextDate}",
                schedule.Id, schedule.Name, schedule.UserId, nextDate);
        }

        if (dueSchedules.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        // 2. Process Category Budget Overrun Alerts
        var categoriesWithCap = await db.Categories
            .AsNoTracking()
            .Where(c => !c.IsArchived && c.MonthlyCap.HasValue && c.MonthlyCap.Value > 0)
            .ToListAsync(ct);

        if (categoriesWithCap.Count > 0)
        {
            var userIds = categoriesWithCap
                .Where(c => c.UserId.HasValue)
                .Select(c => c.UserId!.Value)
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                var userCategories = categoriesWithCap.Where(c => c.UserId == userId).ToList();

                var monthlyExpenses = await db.Entries
                    .AsNoTracking()
                    .Where(e => e.UserId == userId && !e.IsDeleted && e.Date >= startOfMonth && e.Date <= endOfMonth && e.Kind == EntryKind.Expense && e.CategoryId.HasValue)
                    .GroupBy(e => e.CategoryId!.Value)
                    .Select(g => new { CategoryId = g.Key, TotalPaise = g.Sum(e => e.Amount) })
                    .ToListAsync(ct);

                foreach (var cat in userCategories)
                {
                    var spentPaise = monthlyExpenses.FirstOrDefault(m => m.CategoryId == cat.Id)?.TotalPaise ?? 0;
                    if (spentPaise >= cat.MonthlyCap!.Value)
                    {
                        var monthKey = $"{today.Year}-{today.Month:D2}";
                        var alreadyNotified = await db.Notifications.AnyAsync(n =>
                            n.UserId == userId &&
                            n.Type == "budget" &&
                            n.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-3) &&
                            n.Title.Contains(cat.Name), ct);

                        if (!alreadyNotified)
                        {
                            var spent = spentPaise / 100m;
                            var cap = cat.MonthlyCap.Value / 100m;
                            db.Notifications.Add(new Notification
                            {
                                Id = Guid.NewGuid(),
                                UserId = userId,
                                Title = $"Budget Alert: {cat.Name} Over Budget",
                                Message = $"You have spent ₹{spent:N0} in {cat.Name}, exceeding your monthly limit of ₹{cap:N0}.",
                                Type = "budget",
                                Link = "/categories",
                                IsRead = false,
                                CreatedAt = now
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
