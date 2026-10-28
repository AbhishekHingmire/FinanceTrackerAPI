namespace FinanceTrackerAPI.DTOs.Institutions;

public class InstitutionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Bank", "Wallet", "Broker", "Credit", "Cash"
    public string PrimaryColor { get; set; } = "#0f172a";
    public string AccentColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#ffffff";
    public List<string> Aliases { get; set; } = new();
    public string? IconSvg { get; set; }
    public int PopularOrder { get; set; }
}
