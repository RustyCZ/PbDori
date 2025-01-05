using System.Text.Json.Serialization;

namespace PbDori.Model;

public class SymbolTrend
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    [JsonPropertyName("trend")]
    public Trend Trend { get; set; }
}