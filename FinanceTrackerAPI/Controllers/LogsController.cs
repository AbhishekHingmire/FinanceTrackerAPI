namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Mvc;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Services.Interfaces;

public class LogsController : BaseApiController
{
    private readonly IErrorLogService _errorLogService;

    public LogsController(IErrorLogService errorLogService)
    {
        _errorLogService = errorLogService;
    }

    /// <summary>
    /// Retrieves recent error and exception logs recorded in SQL Server.
    /// </summary>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(ApiResponse<List<ErrorLog>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentErrors([FromQuery] int count = 50, CancellationToken ct = default)
    {
        var logs = await _errorLogService.GetRecentErrorsAsync(count, ct);
        return Ok(ApiResponse<List<ErrorLog>>.SuccessResult(logs, $"Retrieved {logs.Count} recent error log(s)."));
    }
}
