using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class Usd
{
    [JsonPropertyName("total_market_cap")]
    public double? TotalMarketCap { get; set; }

    [JsonPropertyName("market_cap")]
    public double? MarketCap { get; set; }
}