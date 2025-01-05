using System.Text.Json.Serialization;

namespace PbDori.Model;

public class MarketTrend
{
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }
    [JsonPropertyName("global_trend")]
    public Trend GlobalTrend { get; set; }
    [JsonPropertyName("bullish_count")]
    public int BullishCount { get; set; }
    [JsonPropertyName("bearish_count")]
    public int BearishCount { get; set; }
    [JsonPropertyName("unknown_count")]
    public int UnknownCount { get; set; }
    [JsonPropertyName("symbol_trends")]
    public SymbolTrend[] SymbolTrends { get; set; } = [];
}