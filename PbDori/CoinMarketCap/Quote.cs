using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class Quote
{
    [JsonPropertyName("USD")]
    public Usd? Usd { get; set; }
}