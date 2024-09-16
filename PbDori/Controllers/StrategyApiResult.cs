using PbDori.Model;
using System.Text.Json.Serialization;

namespace PbDori.Controllers;

public class StrategyApiResult
{
    [JsonPropertyName("strategy_name")]
    public string StrategyName { get; set; } = string.Empty;
    [JsonPropertyName("symbol_data")]
    public SymbolPerformance[] SymbolData { get; set; } = [];
    [JsonPropertyName("total_long_adg")]
    public double TotalLongAdg { get; set; }
    [JsonPropertyName("data_available")]
    public bool DataAvailable { get; set; }
}
