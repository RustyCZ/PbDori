using System.Text.Json.Serialization;

namespace PbDori.Model;

public class BackTestConfig
{
    [JsonPropertyName("market_type")]
    public string MarketType { get; set; } = "futures";

    [JsonPropertyName("user")] 
    public string User { get; set; } = "bybit_01";

    [JsonPropertyName("symbols")] 
    public string[] Symbols { get; set; } = [];

    [JsonPropertyName("latency_simulation_ms")]
    public int LatencySimulationMs { get; set; } = 1000;

    [JsonPropertyName("starting_balance")]
    public double StartingBalance { get; set; } = 100000;

    [JsonPropertyName("start_date")]
    public string? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public string? EndDate { get; set; }

    [JsonPropertyName("slim_analysis")]
    public bool SlimAnalysis { get; set; } = true;

    [JsonPropertyName("base_dir")]
    public string BaseDir { get; set; } = "backtests";

    [JsonPropertyName("ohlcv")] 
    public bool Ohlcv { get; set; } = true;

    [JsonPropertyName("adg_n_subdivisions")]
    public int AdgNSubdivisions { get; set; } = 1;

    [JsonPropertyName("enable_interactive_plot")]
    public bool EnableInteractivePlot { get; set; }

    [JsonPropertyName("plot_theme")]
    public string PlotTheme { get; set; } = "light";

    [JsonPropertyName("plot_candles_interval")]
    public string PlotCandlesInterval { get; set; } = "1m";
}
