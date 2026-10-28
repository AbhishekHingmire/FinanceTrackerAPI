namespace FinanceTrackerAPI.Services.Implementations;

using System.Text.Json;
using FinanceTrackerAPI.Services.Interfaces;

public class MarketDataService : IMarketDataService
{
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
