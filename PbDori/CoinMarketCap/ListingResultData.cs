using System.Text.Json.Serialization;

namespace PbDori.CoinMarketCap;

public class ListingResultData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("cmc_rank")]
    public int CmcRank { get; set; }

    [JsonPropertyName("quote")]
    public Quote? Quote { get; set; }
}