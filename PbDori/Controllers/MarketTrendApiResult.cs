using System.Text.Json.Serialization;
using PbDori.Model;

namespace PbDori.Controllers;

public class MarketTrendApiResult
{
    [JsonPropertyName("market_trend")]
    public MarketTrend? MarketTrend { get; set; }
    [JsonPropertyName("data_available")]
    public bool DataAvailable { get; set; }
}