namespace FinanceTrackerAPI.Services.Implementations;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Services.Interfaces;

public class ErrorLogService : IErrorLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(IServiceScopeFactory scopeFactory, ILogger<ErrorLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogExceptionAsync(
        Exception exception,
        HttpContext? httpContext = null,
        string? controllerName = null,
        string? methodName = null,
        string? additionalInfo = null,
        CancellationToken ct = default)
    {
        try
        {
            Guid? userId = null;
            string? requestPath = null;
            string? httpMethod = null;
            string? ipAddress = null;

            if (httpContext != null)
            {
                requestPath = httpContext.Request.Path.Value;
                httpMethod = httpContext.Request.Method;
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                // Extract UserId from claims if available
                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var parsedGuid))
                {
                    userId = parsedGuid;
                }

                // Extract Controller and Action names from Endpoint metadata if not provided
                if (string.IsNullOrWhiteSpace(controllerName) || string.IsNullOrWhiteSpace(methodName))
                {
                    var endpoint = httpContext.GetEndpoint();
                    var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
                    if (actionDescriptor != null)
                    {
                        controllerName ??= actionDescriptor.ControllerName;
                        methodName ??= actionDescriptor.ActionName;
                    }
                    else if (httpContext.Request.RouteValues.TryGetValue("controller", out var cVal))
                    {
                        controllerName ??= cVal?.ToString();
                        if (httpContext.Request.RouteValues.TryGetValue("action", out var aVal))
                        {
                            methodName ??= aVal?.ToString();
                        }
                    }
                }
            }

            var logEntry = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow,
                LogLevel = "Critical",
                ControllerName = controllerName,
                MethodName = methodName,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                IpAddress = ipAddress,
                AdditionalInfo = additionalInfo ?? (exception.InnerException != null ? $"InnerException: {exception.InnerException.Message}" : null),
                CreatedAt = DateTimeOffset.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceTrackerDbContext>();
            db.ErrorLogs.Add(logEntry);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Fallback: log to console/ILogger so logger failure never hides the original issue
            _logger.LogError(ex, "Failed to persist exception log to database table 'ErrorLogs'");
        }
    }

    public async Task LogErrorAsync(
        string message,
        string? exceptionType = null,
        string? stackTrace = null,
        Guid? userId = null,
        string? controllerName = null,
        string? methodName = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? ipAddress = null,
        string? additionalInfo = null,
        CancellationToken ct = default)
    {
        try
        {
            var logEntry = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Timestamp = DateTimeOffset.UtcNow,
                LogLevel = "Error",
                ControllerName = controllerName,
                MethodName = methodName,
                ExceptionType = exceptionType,
                ExceptionMessage = message,
                StackTrace = stackTrace,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                IpAddress = ipAddress,
                AdditionalInfo = additionalInfo,
                CreatedAt = DateTimeOffset.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinanceTrackerDbContext>();
            db.ErrorLogs.Add(logEntry);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist error log to database table 'ErrorLogs'");
        }
    }

    public async Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceTrackerDbContext>();
        return await db.ErrorLogs
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Clamp(count, 1, 200))
            .ToListAsync(ct);
    }
}
