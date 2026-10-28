namespace FinanceTrackerAPI.Services.Interfaces;

using FinanceTrackerAPI.Entities;
using Microsoft.AspNetCore.Http;

public interface IErrorLogService
{
    Task LogExceptionAsync(
        Exception exception,
        HttpContext? httpContext = null,
        string? controllerName = null,
        string? methodName = null,
        string? additionalInfo = null,
        CancellationToken ct = default);

    Task LogErrorAsync(
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
        CancellationToken ct = default);

    Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50, CancellationToken ct = default);
}
