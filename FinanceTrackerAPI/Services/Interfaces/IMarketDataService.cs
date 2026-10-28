namespace FinanceTrackerAPI.Services.Interfaces;

public class MarketQuoteDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string InstrumentType { get; set; } = "Stock"; // "Stock" or "MutualFund"
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal ChangeAmount { get; set; }
    public decimal ChangePercent { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

public class MutualFundSearchResultDto
{
    public long SchemeCode { get; set; }
    public string SchemeName { get; set; } = string.Empty;
}

public interface IMarketDataService
{
    Task<MarketQuoteDto?> GetQuoteAsync(string symbolOrQuery, string? instrumentType = null, CancellationToken ct = default);
    Task<List<MutualFundSearchResultDto>> SearchMutualFundsAsync(string query, CancellationToken ct = default);
    Task<decimal?> GetMutualFundNavAsync(long schemeCode, CancellationToken ct = default);
}
