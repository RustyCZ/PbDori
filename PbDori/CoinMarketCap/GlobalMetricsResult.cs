using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class GlobalMetricsResult
{
    [JsonPropertyName("status")]
    public Status? Status { get; set; }

    [JsonPropertyName("data")]
    public GlobalMetricsResultData? Data { get; set; }
}