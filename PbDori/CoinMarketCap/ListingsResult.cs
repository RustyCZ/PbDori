using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class ListingsResult
{
    [JsonPropertyName("status")]
    public Status? Status { get; set; }

    [JsonPropertyName("data")]
    public ListingResultData[] Data { get; set; } = [];
}