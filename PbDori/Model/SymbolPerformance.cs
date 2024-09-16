using System.Text.Json.Serialization;

namespace PbDori.Model;

public class SymbolPerformance
{
    [JsonPropertyName("symbol")]
    public required string Symbol { get; set; }

    [JsonPropertyName("volatility")]
    public double Volatility { get; set; }

    [JsonPropertyName("median_volume")]
    public double MedianVolume { get; set; }

    [JsonPropertyName("max_leverage")]
    public double MaxLeverage { get; set; }

    [JsonPropertyName("min_quantity")]
    public double MinQuantity { get; set; }

    [JsonPropertyName("min_notional_value")]
    public double MinNotionalValue { get; set; }

    [JsonPropertyName("copy_trade_enabled")]
    public bool CopyTradeEnabled { get; set; }

    [JsonPropertyName("backtest_last_price")]
    public double BackTestLastPrice { get; set; }

    [JsonPropertyName("backtest_result")]
    public required BackTestResult BackTestResult { get; set; }
}
