using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class CryptoCurrencyInfoResult
{
    [JsonPropertyName("status")]
    public Status? Status { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, CryptoCurrencyInfo> Data { get; set; } = new();
}