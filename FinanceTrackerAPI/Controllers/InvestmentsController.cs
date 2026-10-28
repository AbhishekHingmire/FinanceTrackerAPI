namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinanceTrackerAPI.Data;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Investments;
using FinanceTrackerAPI.Entities;
using FinanceTrackerAPI.Enums;
using FinanceTrackerAPI.Services.Interfaces;

public class InvestmentsController : BaseApiController
{
    private readonly FinanceTrackerDbContext _dbContext;
    private readonly IMarketDataService _marketDataService;
    private readonly ILogger<InvestmentsController> _logger;

    public InvestmentsController(
        FinanceTrackerDbContext dbContext,
        IMarketDataService marketDataService,
        ILogger<InvestmentsController> logger)
    {
        _dbContext = dbContext;
        _marketDataService = marketDataService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InvestmentHoldingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvestments(CancellationToken ct)
    {
        var userId = GetUserId();
        var holdings = await _dbContext.Holdings
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderBy(h => h.Name)
            .ToListAsync(ct);

        return Ok(ApiResponse<List<InvestmentHoldingDto>>.SuccessResult(holdings.Select(MapToDto).ToList()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InvestmentHoldingDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateInvestment([FromBody] CreateInvestmentDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.FailureResult("Validation error", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetUserId();
        var type = dto.AssetType switch
        {
            "Stocks & Equity" => AssetType.StocksEquity,
            "Fixed deposit" => AssetType.FixedDeposit,
            "Gold & Commodities" => AssetType.GoldCommodities,
            "Crypto" => AssetType.Crypto,
            _ => AssetType.MutualFund
        };

        DateOnly? startDate = null;
        if (!string.IsNullOrWhiteSpace(dto.StartMonthYear) && DateOnly.TryParse($"{dto.StartMonthYear}-01", out var sd))
        {
            startDate = sd;
        }

        var holding = new Holding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = type,
            Broker = dto.Institution?.Trim() ?? string.Empty,
            OpeningInvested = (long)(dto.InvestedAmount * 100),
            CurrentValue = (long)(dto.CurrentAmount * 100),
            StartDate = startDate,
            HasSip = dto.IsRecurring,
            SipAmount = dto.SipAmount.HasValue ? (long)(dto.SipAmount.Value * 100) : null,
            SipFromAccountId = dto.SipAccountId,
            SipActive = dto.Status == "Active",
            Notes = dto.Notes
        };

        _dbContext.Holdings.Add(holding);
        await _dbContext.SaveChangesAsync(ct);

        return Ok(ApiResponse<InvestmentHoldingDto>.SuccessResult(MapToDto(holding), "Investment added successfully"));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<InvestmentHoldingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateInvestment(Guid id, [FromBody] UpdateInvestmentDto dto, CancellationToken ct)
    {
        var userId = GetUserId();
        var holding = await _dbContext.Holdings
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, ct);

        if (holding == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Holding not found"));
        }

        holding.Name = dto.Name.Trim();
        holding.Broker = dto.Institution?.Trim() ?? string.Empty;
        holding.OpeningInvested = (long)(dto.InvestedAmount * 100);
        holding.CurrentValue = (long)(dto.CurrentAmount * 100);
        holding.HasSip = dto.IsRecurring;
        holding.SipAmount = dto.SipAmount.HasValue ? (long)(dto.SipAmount.Value * 100) : null;
        holding.SipFromAccountId = dto.SipAccountId;
        holding.SipActive = dto.Status == "Active";
        holding.Notes = dto.Notes;

        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<InvestmentHoldingDto>.SuccessResult(MapToDto(holding), "Holding updated"));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteInvestment(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var holding = await _dbContext.Holdings
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId, ct);

        if (holding == null)
        {
            return NotFound(ApiResponse<object>.FailureResult("Holding not found"));
        }

        _dbContext.Holdings.Remove(holding);
        await _dbContext.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.SuccessResult(true, "Holding deleted"));
    }

    /// <summary>
    /// Searches national and international stocks, mutual funds, ETFs, and gold.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<InstrumentSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchInstruments([FromQuery] string query = "", [FromQuery] string? category = null, CancellationToken ct = default)
    {
        var results = await _marketDataService.SearchAllInstrumentsAsync(query, category, ct);
        return Ok(ApiResponse<List<InstrumentSearchResultDto>>.SuccessResult(results));
    }

    /// <summary>
    /// Fetches live accurate market price or NAV for any stock (e.g. RELIANCE, TCS, INFY) or Mutual Fund.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("quote")]
    [ProducesResponseType(typeof(ApiResponse<MarketQuoteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetQuote([FromQuery] string query, [FromQuery] string? type = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(ApiResponse<object>.FailureResult("Query/symbol is required"));
        }

        var quote = await _marketDataService.GetQuoteAsync(query, type, ct);
        if (quote == null)
        {
            return NotFound(ApiResponse<object>.FailureResult($"No live quote found for '{query}'"));
        }

        return Ok(ApiResponse<MarketQuoteDto>.SuccessResult(quote));
    }

    /// <summary>
    /// Searches Indian Mutual Funds registered with AMFI via the official public directory.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("mutual-funds/search")]
    [ProducesResponseType(typeof(ApiResponse<List<MutualFundSearchResultDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchMutualFunds([FromQuery] string query, CancellationToken ct = default)
    {
        var results = await _marketDataService.SearchMutualFundsAsync(query, ct);
        return Ok(ApiResponse<List<MutualFundSearchResultDto>>.SuccessResult(results));
    }

    /// <summary>
    /// Retrieves the official live NAV for a Mutual Fund by AMFI Scheme Code.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("mutual-funds/{schemeCode:long}/nav")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMutualFundNav(long schemeCode, CancellationToken ct = default)
    {
        var nav = await _marketDataService.GetMutualFundNavAsync(schemeCode, ct);
        if (!nav.HasValue)
        {
            return NotFound(ApiResponse<object>.FailureResult("NAV not available for this scheme code"));
        }

        return Ok(ApiResponse<decimal>.SuccessResult(nav.Value));
    }

    private static InvestmentHoldingDto MapToDto(Holding h) => new()
    {
        Id = h.Id,
        Name = h.Name,
        AssetType = h.Type switch
        {
            AssetType.StocksEquity => "Stocks & Equity",
            AssetType.FixedDeposit => "Fixed deposit",
            AssetType.GoldCommodities => "Gold & Commodities",
            AssetType.Crypto => "Crypto",
            _ => "Mutual fund"
        },
        InvestedAmount = h.OpeningInvested / 100m,
        CurrentAmount = h.CurrentValue / 100m,
        StartMonthYear = h.StartDate?.ToString("yyyy-MM"),
        Institution = h.Broker,
        Notes = h.Notes,
        UpdatedAt = DateTimeOffset.UtcNow.ToString("o"),
        IsRecurring = h.HasSip,
        SipAmount = (h.SipAmount ?? 0) / 100m,
        SipAccountId = h.SipFromAccountId?.ToString(),
        Status = h.SipActive ? "Active" : "Paused"
    };
}
