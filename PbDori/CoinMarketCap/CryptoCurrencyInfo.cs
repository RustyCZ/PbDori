using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class CryptoCurrencyInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("notice")]
    public string Notice { get; set; } = string.Empty;
}