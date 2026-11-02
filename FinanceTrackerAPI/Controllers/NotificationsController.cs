namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Entities;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
}

public class NotificationsController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(FinanceTrackerDbContext dbContext, ILogger<NotificationsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? unreadOnly, CancellationToken ct)
    {
        var userId = GetUserId();
        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt.ToString("o")
            })
            .ToListAsync(ct);

        return Ok(ApiResponse<List<NotificationDto>>.SuccessResult(items));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var notif = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        if (notif == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Notification not found"));
        }

        notif.IsRead = true;
        notif.ReadAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<bool>.SuccessResult(true, "Marked as read"));
    }

    [HttpPost("read-all")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetUserId();
        var unread = await _dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var item in unread)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "All notifications marked as read"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var notif = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);

        if (notif == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Notification not found"));
        }

        _dbContext.Notifications.Remove(notif);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<bool>.SuccessResult(true, "Notification deleted"));
    }
}
