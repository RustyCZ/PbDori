using System.Text.Json.Serialization;

namespace PbDori.Model;

public class BackTestResult
{
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime EndDate { get; set; }

    [JsonPropertyName("n_days")]
    public double NumberOfDays { get; set; }

    [JsonPropertyName("result")] 
    public BackTestSummary Result { get; set; } = new BackTestSummary();
}
