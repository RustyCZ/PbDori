using System.Text.Json.Serialization;

namespace PbDori.Model;

public class BackTestSummary
{
    [JsonPropertyName("adg_long")]
    public double? AdgLong { get; set; }
    [JsonPropertyName("adg_per_exposure_long")]
    public double? AdgPerExposureLong { get; set; }
    [JsonPropertyName("adg_per_exposure_short")]
    public double? AdgPerExposureShort { get; set; }
    [JsonPropertyName("adg_short")]
    public double? AdgShort { get; set; }
    [JsonPropertyName("adg_weighted_long")]
    public double? AdgWeightedLong { get; set; }
    [JsonPropertyName("adg_weighted_per_exposure_long")]
    public double? AdgWeightedPerExposureLong { get; set; }
    [JsonPropertyName("adg_weighted_per_exposure_short")]
    public double? AdgWeightedPerExposureShort { get; set; }
    [JsonPropertyName("adg_weighted_short")]
    public double? AdgWeightedShort { get; set; }
    [JsonPropertyName("symbol")]
    public string? Symbol { get; set; }
}