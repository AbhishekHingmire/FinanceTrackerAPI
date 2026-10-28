namespace FinanceTrackerAPI.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinanceTrackerAPI.DTOs.Common;
using FinanceTrackerAPI.DTOs.Institutions;

[AllowAnonymous]
public class InstitutionsController : BaseApiController
{
    private static readonly List<InstitutionDto> MasterInstitutions = new()
    {
        // Major Scheduled Commercial Banks
        new()
        {
            Id = "HDFC",
            Name = "HDFC Bank",
            ShortName = "HDFC",
            Category = "Bank",
            PrimaryColor = "#004c8f",
            AccentColor = "#ed1c24",
            TextColor = "#ffffff",
            PopularOrder = 1,
            Aliases = new() { "hdfc", "hdfc bank", "hdfc bank ltd", "hdfc-bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"8\" fill=\"#004c8f\"/><rect x=\"8\" y=\"8\" width=\"32\" height=\"32\" rx=\"4\" fill=\"#ed1c24\"/><rect x=\"15\" y=\"15\" width=\"18\" height=\"18\" rx=\"2\" fill=\"#ffffff\"/><rect x=\"21\" y=\"7\" width=\"6\" height=\"34\" fill=\"#004c8f\"/><rect x=\"7\" y=\"21\" width=\"34\" height=\"6\" fill=\"#004c8f\"/></svg>"
        },
        new()
        {
            Id = "SBI",
            Name = "State Bank of India (SBI)",
            ShortName = "SBI",
            Category = "Bank",
            PrimaryColor = "#0082c9",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 2,
            Aliases = new() { "sbi", "state bank of india", "state bank", "sbi bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><circle cx=\"24\" cy=\"24\" r=\"23\" fill=\"#0082c9\"/><circle cx=\"24\" cy=\"18\" r=\"9\" fill=\"#ffffff\"/><rect x=\"21\" y=\"18\" width=\"6\" height=\"24\" rx=\"2\" fill=\"#0082c9\"/></svg>"
        },
        new()
        {
            Id = "ICICI",
            Name = "ICICI Bank",
            ShortName = "ICICI",
            Category = "Bank",
            PrimaryColor = "#9c1f24",
            AccentColor = "#f37021",
            TextColor = "#ffffff",
            PopularOrder = 3,
            Aliases = new() { "icici", "icici bank", "icici bank ltd", "icici-bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#9c1f24\"/><path d=\"M 12 36 C 12 18, 30 14, 38 18 C 30 20, 20 26, 20 36 Z\" fill=\"#f37021\"/><circle cx=\"34\" cy=\"16\" r=\"4.5\" fill=\"#ffffff\"/><path d=\"M 31 23 H 37 V 36 H 31 Z\" fill=\"#ffffff\"/></svg>"
        },
        new()
        {
            Id = "AXIS",
            Name = "Axis Bank",
            ShortName = "Axis",
            Category = "Bank",
            PrimaryColor = "#97144d",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 4,
            Aliases = new() { "axis", "axis bank", "axis bank ltd", "uti bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#97144d\"/><polygon points=\"24,9 38,37 29,37 24,24 19,37 10,37\" fill=\"#ffffff\"/><polygon points=\"24,17 31,33 27,33 24,26 21,33 17,33\" fill=\"#97144d\"/></svg>"
        },
        new()
        {
            Id = "KOTAK",
            Name = "Kotak Mahindra Bank",
            ShortName = "Kotak",
            Category = "Bank",
            PrimaryColor = "#ed1c24",
            AccentColor = "#003366",
            TextColor = "#ffffff",
            PopularOrder = 5,
            Aliases = new() { "kotak", "kotak mahindra", "kotak bank", "kotak 811" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#ed1c24\"/><path d=\"M 15 24 C 15 17, 24 17, 24 24 C 24 31, 33 31, 33 24 C 33 17, 24 17, 24 24 C 24 31, 15 31, 15 24 Z\" fill=\"none\" stroke=\"#ffffff\" stroke-width=\"5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>"
        },
        new()
        {
            Id = "PNB",
            Name = "Punjab National Bank (PNB)",
            ShortName = "PNB",
            Category = "Bank",
            PrimaryColor = "#a21d22",
            AccentColor = "#ffcb05",
            TextColor = "#ffffff",
            PopularOrder = 6,
            Aliases = new() { "pnb", "punjab national bank", "punjab national" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#a21d22\"/><circle cx=\"24\" cy=\"24\" r=\"14\" fill=\"#ffcb05\"/><circle cx=\"24\" cy=\"24\" r=\"8\" fill=\"#a21d22\"/><rect x=\"22\" y=\"10\" width=\"4\" height=\"28\" fill=\"#ffcb05\"/></svg>"
        },
        new()
        {
            Id = "BOB",
            Name = "Bank of Baroda",
            ShortName = "BOB",
            Category = "Bank",
            PrimaryColor = "#f26522",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 7,
            Aliases = new() { "bob", "bank of baroda", "baroda" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#f26522\"/><circle cx=\"24\" cy=\"24\" r=\"13\" fill=\"#ffffff\"/><path d=\"M 24 14 L 27 21 L 34 24 L 27 27 L 24 34 L 21 27 L 14 24 L 21 21 Z\" fill=\"#f26522\"/></svg>"
        },
        new()
        {
            Id = "CANARA",
            Name = "Canara Bank",
            ShortName = "Canara",
            Category = "Bank",
            PrimaryColor = "#0091df",
            AccentColor = "#ffb81c",
            TextColor = "#ffffff",
            PopularOrder = 8,
            Aliases = new() { "canara", "canara bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#0091df\"/><polygon points=\"12,34 26,12 36,34\" fill=\"#ffb81c\"/><polygon points=\"18,34 28,17 38,34\" fill=\"#ffffff\" fill-opacity=\"0.85\"/></svg>"
        },
        new()
        {
            Id = "UNION",
            Name = "Union Bank of India",
            ShortName = "Union Bank",
            Category = "Bank",
            PrimaryColor = "#e31e24",
            AccentColor = "#0054a6",
            TextColor = "#ffffff",
            PopularOrder = 9,
            Aliases = new() { "union bank", "union bank of india", "ubi" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#ffffff\"/><path d=\"M 13 14 V 27 C 13 33, 19 37, 24 37 C 29 37, 35 33, 35 27 V 14\" fill=\"none\" stroke=\"#e31e24\" stroke-width=\"5\" stroke-linecap=\"round\"/><path d=\"M 19 14 V 24 C 19 27, 21 29, 24 29 C 27 29, 29 27, 29 24 V 14\" fill=\"none\" stroke=\"#0054a6\" stroke-width=\"4\" stroke-linecap=\"round\"/></svg>"
        },
        new()
        {
            Id = "INDUSIND",
            Name = "IndusInd Bank",
            ShortName = "IndusInd",
            Category = "Bank",
            PrimaryColor = "#981e32",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 10,
            Aliases = new() { "indusind", "indusind bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#981e32\"/><circle cx=\"24\" cy=\"24\" r=\"14\" fill=\"#ffffff\" fill-opacity=\"0.15\"/><path d=\"M 14 28 Q 18 16 28 20 Q 34 22 36 28 Q 28 26 22 28 Z\" fill=\"#ffffff\"/><circle cx=\"31\" cy=\"18\" r=\"2.5\" fill=\"#ffffff\"/></svg>"
        },
        new()
        {
            Id = "YES_BANK",
            Name = "Yes Bank",
            ShortName = "Yes Bank",
            Category = "Bank",
            PrimaryColor = "#005ba6",
            AccentColor = "#ed1b24",
            TextColor = "#ffffff",
            PopularOrder = 11,
            Aliases = new() { "yes bank", "yes-bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#005ba6\"/><path d=\"M 13 24 L 21 32 L 35 15\" fill=\"none\" stroke=\"#ed1b24\" stroke-width=\"5.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>"
        },
        new()
        {
            Id = "IDFC",
            Name = "IDFC FIRST Bank",
            ShortName = "IDFC FIRST",
            Category = "Bank",
            PrimaryColor = "#9d1d27",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 12,
            Aliases = new() { "idfc", "idfc first", "idfc bank", "idfc first bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#9d1d27\"/><rect x=\"12\" y=\"12\" width=\"10\" height=\"10\" rx=\"2\" fill=\"#ffffff\"/><rect x=\"26\" y=\"12\" width=\"10\" height=\"10\" rx=\"2\" fill=\"#ffffff\" fill-opacity=\"0.8\"/><rect x=\"12\" y=\"26\" width=\"10\" height=\"10\" rx=\"2\" fill=\"#ffffff\" fill-opacity=\"0.8\"/><rect x=\"26\" y=\"26\" width=\"10\" height=\"10\" rx=\"2\" fill=\"#ffffff\"/></svg>"
        },

        // Digital Wallets & UPI
        new()
        {
            Id = "PAYTM",
            Name = "Paytm",
            ShortName = "Paytm",
            Category = "Wallet",
            PrimaryColor = "#002e6e",
            AccentColor = "#00b9f5",
            TextColor = "#ffffff",
            PopularOrder = 13,
            Aliases = new() { "paytm", "paytm wallet", "paytm payments bank", "paytm bank" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#002e6e\"/><text x=\"24\" y=\"32\" fill=\"#00b9f5\" font-size=\"24\" font-weight=\"900\" font-family=\"sans-serif\" text-anchor=\"middle\">P</text></svg>"
        },
        new()
        {
            Id = "PHONEPE",
            Name = "PhonePe",
            ShortName = "PhonePe",
            Category = "Wallet",
            PrimaryColor = "#5f259f",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 14,
            Aliases = new() { "phonepe", "phone pe", "phonepe wallet" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"11\" fill=\"#5f259f\"/><text x=\"24\" y=\"32\" fill=\"#ffffff\" font-size=\"24\" font-weight=\"bold\" font-family=\"sans-serif\" text-anchor=\"middle\">पे</text></svg>"
        },
        new()
        {
            Id = "GPAY",
            Name = "Google Pay",
            ShortName = "GPay",
            Category = "Wallet",
            PrimaryColor = "#4285f4",
            AccentColor = "#34a853",
            TextColor = "#ffffff",
            PopularOrder = 15,
            Aliases = new() { "gpay", "google pay", "tez", "googlepay" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"11\" fill=\"#ffffff\" stroke=\"#e2e8f0\" stroke-width=\"1.5\"/><path d=\"M 24 12 C 30 12, 34 14.5, 36 17 L 31 21 C 29.5 19.5, 27 18, 24 18 C 19 18, 15 21.5, 14 26 C 13.5 24 13.5 22 14 20 C 16 15, 20 12, 24 12 Z\" fill=\"#ea4335\"/><path d=\"M 36 17 L 31 21 C 32 23 32.5 25 32.5 27 C 32.5 28 32.2 29 31.8 30 L 36.5 33.5 C 38 31 39 28 39 25 C 39 22 38 19 36 17 Z\" fill=\"#4285f4\"/><path d=\"M 24 36 C 28.5 36, 33 34.5, 36.5 32 L 31.8 28.5 C 30 30, 27 30.5, 24 30.5 C 19.5 30.5, 16 28, 14 24 L 9.5 27.5 C 12.5 33, 18 36, 24 36 Z\" fill=\"#34a853\"/><path d=\"M 14 24 C 13.5 22.5, 13.5 20.5, 14 19 L 9.5 15.5 C 8 19 8 24 9.5 27.5 L 14 24 Z\" fill=\"#fbbc05\"/></svg>"
        },
        new()
        {
            Id = "AMAZON_PAY",
            Name = "Amazon Pay",
            ShortName = "Amazon Pay",
            Category = "Wallet",
            PrimaryColor = "#232f3e",
            AccentColor = "#ff9900",
            TextColor = "#ffffff",
            PopularOrder = 16,
            Aliases = new() { "amazon pay", "amazonpay", "amazon" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#232f3e\"/><text x=\"24\" y=\"24\" fill=\"#ffffff\" font-size=\"16\" font-weight=\"bold\" text-anchor=\"middle\" font-family=\"sans-serif\">pay</text><path d=\"M 12 30 C 18 36, 30 36, 36 30\" fill=\"none\" stroke=\"#ff9900\" stroke-width=\"3.5\" stroke-linecap=\"round\"/></svg>"
        },
        new()
        {
            Id = "CRED",
            Name = "CRED Balance",
            ShortName = "CRED",
            Category = "Wallet",
            PrimaryColor = "#171717",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 17,
            Aliases = new() { "cred", "cred cash", "cred pay", "cred balance" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#171717\" stroke=\"#333333\" stroke-width=\"1\"/><path d=\"M 16 16 H 32 V 28 C 32 34, 24 37, 24 37 C 24 37, 16 34, 16 28 Z\" fill=\"none\" stroke=\"#ffffff\" stroke-width=\"3\"/><circle cx=\"24\" cy=\"24\" r=\"3\" fill=\"#ffffff\"/></svg>"
        },

        // Brokers & Investment Platforms
        new()
        {
            Id = "ZERODHA",
            Name = "Zerodha Kite",
            ShortName = "Zerodha",
            Category = "Broker",
            PrimaryColor = "#387ed1",
            AccentColor = "#ffffff",
            TextColor = "#ffffff",
            PopularOrder = 18,
            Aliases = new() { "zerodha", "kite", "zerodha kite" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#387ed1\"/><polygon points=\"24,10 36,24 24,38 12,24\" fill=\"#ffffff\"/><polygon points=\"24,14 32,24 24,34 16,24\" fill=\"#387ed1\" fill-opacity=\"0.4\"/></svg>"
        },
        new()
        {
            Id = "GROWW",
            Name = "Groww",
            ShortName = "Groww",
            Category = "Broker",
            PrimaryColor = "#00d09c",
            AccentColor = "#5367ff",
            TextColor = "#ffffff",
            PopularOrder = 19,
            Aliases = new() { "groww", "groww app", "groww invest" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#00d09c\"/><path d=\"M 13 32 L 20 22 L 27 28 L 35 16\" fill=\"none\" stroke=\"#ffffff\" stroke-width=\"5\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/></svg>"
        },

        // Cash
        new()
        {
            Id = "CASH",
            Name = "Physical Cash",
            ShortName = "Cash",
            Category = "Cash",
            PrimaryColor = "#059669",
            AccentColor = "#10b981",
            TextColor = "#ffffff",
            PopularOrder = 20,
            Aliases = new() { "cash", "cash wallet", "hand cash", "physical cash", "currency", "petty cash" },
            IconSvg = "<svg viewBox=\"0 0 48 48\"><rect width=\"48\" height=\"48\" rx=\"10\" fill=\"#059669\"/><rect x=\"8\" y=\"14\" width=\"32\" height=\"20\" rx=\"3\" fill=\"#10b981\" stroke=\"#ffffff\" stroke-width=\"2\"/><circle cx=\"24\" cy=\"24\" r=\"5\" fill=\"#ffffff\"/></svg>"
        }
    };

    /// <summary>
    /// Returns public financial institutions, banks, wallets, and broker platforms with real branding icons and colors.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<InstitutionDto>>), StatusCodes.Status200OK)]
    public IActionResult GetInstitutions([FromQuery] string? category = null)
    {
        var items = MasterInstitutions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            items = items.Where(i => i.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        var result = items.OrderBy(i => i.PopularOrder).ToList();
        return Ok(ApiResponse<List<InstitutionDto>>.SuccessResult(result, $"Fetched {result.Count} financial institutions."));
    }
}
