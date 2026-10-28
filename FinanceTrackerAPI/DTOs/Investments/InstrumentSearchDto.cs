namespace FinanceTrackerAPI.DTOs.Investments;

public class InstrumentSearchResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Stock"; // "National Stock", "International Stock", "Mutual Fund", "ETF", "Gold & Commodities"
    public string AssetType { get; set; } = "Stocks & Equity"; // "Stocks & Equity", "Mutual fund", "ETF", "Gold & Commodities"
    public string Exchange { get; set; } = "NSE";
    public decimal? CurrentPrice { get; set; }
    public string Currency { get; set; } = "INR";
}
