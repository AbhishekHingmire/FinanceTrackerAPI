namespace FinanceTrackerAPI.Services.Implementations;

using System.Text.Json;
using FinanceTrackerAPI.Services.Interfaces;

public class MarketDataService : IMarketDataService
{
    private static readonly List<FinanceTrackerAPI.DTOs.Investments.InstrumentSearchResultDto> MasterInstruments = new()
    {
        // National Stocks (India - NSE)
        new() { Symbol = "RELIANCE.NS", Name = "Reliance Industries Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1270.50m, Currency = "INR" },
        new() { Symbol = "TCS.NS", Name = "Tata Consultancy Services Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 2460.00m, Currency = "INR" },
        new() { Symbol = "HDFCBANK.NS", Name = "HDFC Bank Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1780.20m, Currency = "INR" },
        new() { Symbol = "ICICIBANK.NS", Name = "ICICI Bank Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1265.80m, Currency = "INR" },
        new() { Symbol = "INFY.NS", Name = "Infosys Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1845.00m, Currency = "INR" },
        new() { Symbol = "SBIN.NS", Name = "State Bank of India", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 820.40m, Currency = "INR" },
        new() { Symbol = "BHARTIARTL.NS", Name = "Bharti Airtel Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1690.00m, Currency = "INR" },
        new() { Symbol = "ITC.NS", Name = "ITC Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 475.60m, Currency = "INR" },
        new() { Symbol = "LT.NS", Name = "Larsen & Toubro Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 3650.00m, Currency = "INR" },
        new() { Symbol = "TATAMOTORS.NS", Name = "Tata Motors Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 875.00m, Currency = "INR" },
        new() { Symbol = "HINDUNILVR.NS", Name = "Hindustan Unilever Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 2390.00m, Currency = "INR" },
        new() { Symbol = "BAJFINANCE.NS", Name = "Bajaj Finance Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 6980.00m, Currency = "INR" },
        new() { Symbol = "AXISBANK.NS", Name = "Axis Bank Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1195.00m, Currency = "INR" },
        new() { Symbol = "KOTAKBANK.NS", Name = "Kotak Mahindra Bank Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1760.00m, Currency = "INR" },
        new() { Symbol = "WIPRO.NS", Name = "Wipro Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 540.00m, Currency = "INR" },
        new() { Symbol = "SUNPHARMA.NS", Name = "Sun Pharmaceutical Industries Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 1880.00m, Currency = "INR" },
        new() { Symbol = "MARUTI.NS", Name = "Maruti Suzuki India Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 12450.00m, Currency = "INR" },
        new() { Symbol = "TITAN.NS", Name = "Titan Company Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 3280.00m, Currency = "INR" },
        new() { Symbol = "ZOMATO.NS", Name = "Zomato Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 265.00m, Currency = "INR" },
        new() { Symbol = "JIOFIN.NS", Name = "Jio Financial Services Ltd", Category = "National Stock", AssetType = "Stocks & Equity", Exchange = "NSE", CurrentPrice = 315.00m, Currency = "INR" },

        // International Stocks (US / Global - NASDAQ / NYSE)
        new() { Symbol = "AAPL", Name = "Apple Inc.", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 230.50m, Currency = "USD" },
        new() { Symbol = "MSFT", Name = "Microsoft Corporation", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 428.00m, Currency = "USD" },
        new() { Symbol = "GOOGL", Name = "Alphabet Inc. (Google)", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 168.00m, Currency = "USD" },
        new() { Symbol = "AMZN", Name = "Amazon.com Inc.", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 186.00m, Currency = "USD" },
        new() { Symbol = "NVDA", Name = "NVIDIA Corporation", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 140.00m, Currency = "USD" },
        new() { Symbol = "META", Name = "Meta Platforms Inc.", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 585.00m, Currency = "USD" },
        new() { Symbol = "TSLA", Name = "Tesla Inc.", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NASDAQ", CurrentPrice = 260.00m, Currency = "USD" },
        new() { Symbol = "BRK-B", Name = "Berkshire Hathaway Inc.", Category = "International Stock", AssetType = "Stocks & Equity", Exchange = "NYSE", CurrentPrice = 455.00m, Currency = "USD" },

        // ETFs & Gold & Commodities
        new() { Symbol = "GOLDBEES.NS", Name = "Nippon India ETF Gold BeES", Category = "Gold & Commodities", AssetType = "Gold & Commodities", Exchange = "NSE", CurrentPrice = 72.50m, Currency = "INR" },
        new() { Symbol = "SILVERBEES.NS", Name = "Nippon India ETF Silver BeES", Category = "Gold & Commodities", AssetType = "Gold & Commodities", Exchange = "NSE", CurrentPrice = 88.00m, Currency = "INR" },
        new() { Symbol = "NIFTYBEES.NS", Name = "Nippon India ETF Nifty 50 BeES", Category = "ETF", AssetType = "ETF", Exchange = "NSE", CurrentPrice = 265.20m, Currency = "INR" },
        new() { Symbol = "MON100.NS", Name = "Motilal Oswal Nasdaq 100 ETF", Category = "ETF", AssetType = "ETF", Exchange = "NSE", CurrentPrice = 165.00m, Currency = "INR" },
        new() { Symbol = "MASPTOP50.NS", Name = "Mirae Asset S&P 500 Top 50 ETF", Category = "ETF", AssetType = "ETF", Exchange = "NSE", CurrentPrice = 52.00m, Currency = "INR" },
        new() { Symbol = "CPSEETF.NS", Name = "CPSE ETF", Category = "ETF", AssetType = "ETF", Exchange = "NSE", CurrentPrice = 94.50m, Currency = "INR" }
    };

    public async Task<List<FinanceTrackerAPI.DTOs.Investments.InstrumentSearchResultDto>> SearchAllInstrumentsAsync(
        string query,
        string? category = null,
        CancellationToken ct = default)
    {
        var cleanQ = query?.Trim() ?? string.Empty;
        var results = new List<FinanceTrackerAPI.DTOs.Investments.InstrumentSearchResultDto>();

        // 1. Filter local master instruments
        var matches = MasterInstruments.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(cleanQ))
        {
            matches = matches.Where(i =>
                i.Symbol.Contains(cleanQ, StringComparison.OrdinalIgnoreCase) ||
                i.Name.Contains(cleanQ, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            matches = matches.Where(i => i.Category.Contains(category, StringComparison.OrdinalIgnoreCase) ||
                                         i.AssetType.Contains(category, StringComparison.OrdinalIgnoreCase));
        }

        results.AddRange(matches.Take(15));

        // 2. Query AMFI Mutual Funds live API if query length >= 2
        if (cleanQ.Length >= 2 && (string.IsNullOrWhiteSpace(category) || category.Contains("fund", StringComparison.OrdinalIgnoreCase)))
        {
            var mfMatches = await SearchMutualFundsAsync(cleanQ, ct);
            foreach (var mf in mfMatches.Take(10))
            {
                results.Add(new FinanceTrackerAPI.DTOs.Investments.InstrumentSearchResultDto
                {
                    Symbol = mf.SchemeCode.ToString(),
                    Name = mf.SchemeName,
                    Category = "Mutual Fund",
                    AssetType = "Mutual fund",
                    Exchange = "AMFI",
                    Currency = "INR"
                });
            }
        }

        return results;
    }
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarketDataService> _logger;

    public MarketDataService(HttpClient httpClient, ILogger<MarketDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "WealthTrack/1.0 (Windows NT 10.0; Win64; x64)");
    }

    public async Task<MarketQuoteDto?> GetQuoteAsync(string symbolOrQuery, string? instrumentType = null, CancellationToken ct = default)
    {
        var query = symbolOrQuery?.Trim();
        if (string.IsNullOrWhiteSpace(query)) return null;

        // 1. If instrument type is Mutual Fund or query is numeric scheme code
        if (long.TryParse(query, out var schemeCode) || instrumentType?.Equals("MutualFund", StringComparison.OrdinalIgnoreCase) == true)
        {
            if (schemeCode > 0)
            {
                var nav = await GetMutualFundNavAsync(schemeCode, ct);
                if (nav.HasValue)
                {
                    return new MarketQuoteDto
                    {
                        Symbol = schemeCode.ToString(),
                        Name = $"Mutual Fund {schemeCode}",
                        InstrumentType = "MutualFund",
                        CurrentPrice = nav.Value,
                        Currency = "INR",
                        LastUpdated = DateTimeOffset.UtcNow
                    };
                }
            }

            // Search by name
            var searchResults = await SearchMutualFundsAsync(query, ct);
            if (searchResults.Any())
            {
                var first = searchResults.First();
                var nav = await GetMutualFundNavAsync(first.SchemeCode, ct);
                if (nav.HasValue)
                {
                    return new MarketQuoteDto
                    {
                        Symbol = first.SchemeCode.ToString(),
                        Name = first.SchemeName,
                        InstrumentType = "MutualFund",
                        CurrentPrice = nav.Value,
                        Currency = "INR",
                        LastUpdated = DateTimeOffset.UtcNow
                    };
                }
            }
        }

        // 2. Stock / Equity Quote using Yahoo Finance public chart API
        try
        {
            var symbol = query.ToUpperInvariant();
            if (!symbol.Contains('.') && !symbol.Contains('^'))
            {
                symbol = $"{symbol}.NS"; // Default to National Stock Exchange (NSE)
            }

            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=5d";
            var res = await _httpClient.GetAsync(url, ct);

            if (!res.IsSuccessStatusCode && symbol.EndsWith(".NS"))
            {
                // Fallback to BSE (.BO)
                symbol = symbol.Replace(".NS", ".BO");
                url = $"https://query1.finance.yahoo.com/v8/finance/chart/{symbol}?interval=1d&range=5d";
                res = await _httpClient.GetAsync(url, ct);
            }

            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var chart = doc.RootElement.GetProperty("chart");
                var results = chart.GetProperty("result");

                if (results.GetArrayLength() > 0)
                {
                    var meta = results[0].GetProperty("meta");
                    var currentPrice = meta.GetProperty("regularMarketPrice").GetDecimal();
                    var prevClose = meta.TryGetProperty("chartPreviousClose", out var prevProp)
                        ? prevProp.GetDecimal()
                        : currentPrice;

                    var currency = meta.TryGetProperty("currency", out var currProp)
                        ? currProp.GetString() ?? "INR"
                        : "INR";

                    var change = currentPrice - prevClose;
                    var changePercent = prevClose > 0 ? (change / prevClose) * 100 : 0;

                    return new MarketQuoteDto
                    {
                        Symbol = symbol,
                        Name = meta.TryGetProperty("shortName", out var nameProp) ? nameProp.GetString() ?? symbol : symbol,
                        InstrumentType = "Stock",
                        CurrentPrice = Math.Round(currentPrice, 2),
                        PreviousClose = Math.Round(prevClose, 2),
                        ChangeAmount = Math.Round(change, 2),
                        ChangePercent = Math.Round(changePercent, 2),
                        Currency = currency,
                        LastUpdated = DateTimeOffset.UtcNow
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve stock quote for {Query}", query);
        }

        return null;
    }

    public async Task<List<MutualFundSearchResultDto>> SearchMutualFundsAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<MutualFundSearchResultDto>();

        try
        {
            var url = $"https://api.mfapi.in/mf/search?q={Uri.EscapeDataString(query.Trim())}";
            var res = await _httpClient.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode) return new List<MutualFundSearchResultDto>();

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var list = new List<MutualFundSearchResultDto>();
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray().Take(20))
                {
                    if (item.TryGetProperty("schemeCode", out var codeProp) &&
                        item.TryGetProperty("schemeName", out var nameProp))
                    {
                        long code = codeProp.ValueKind == JsonValueKind.Number
                            ? codeProp.GetInt64()
                            : long.TryParse(codeProp.GetString(), out var parsed) ? parsed : 0;

                        if (code > 0)
                        {
                            list.Add(new MutualFundSearchResultDto
                            {
                                SchemeCode = code,
                                SchemeName = nameProp.GetString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search mutual funds for query {Query}", query);
            return new List<MutualFundSearchResultDto>();
        }
    }

    public async Task<decimal?> GetMutualFundNavAsync(long schemeCode, CancellationToken ct = default)
    {
        if (schemeCode <= 0) return null;

        try
        {
            var url = $"https://api.mfapi.in/mf/{schemeCode}/latest";
            var res = await _httpClient.GetAsync(url, ct);
            if (!res.IsSuccessStatusCode)
            {
                // Try standard endpoint
                url = $"https://api.mfapi.in/mf/{schemeCode}";
                res = await _httpClient.GetAsync(url, ct);
            }

            if (!res.IsSuccessStatusCode) return null;

            var json = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var dataProp) &&
                dataProp.ValueKind == JsonValueKind.Array &&
                dataProp.GetArrayLength() > 0)
            {
                var latestRecord = dataProp[0];
                if (latestRecord.TryGetProperty("nav", out var navProp))
                {
                    var navStr = navProp.GetString();
                    if (decimal.TryParse(navStr, out var navVal))
                    {
                        return Math.Round(navVal, 4);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch mutual fund NAV for scheme {SchemeCode}", schemeCode);
        }

        return null;
    }
}
