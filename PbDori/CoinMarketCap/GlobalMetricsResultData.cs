using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class GlobalMetricsResultData
{
    [JsonPropertyName("quote")]
    public Quote? Quote { get; set; }
}