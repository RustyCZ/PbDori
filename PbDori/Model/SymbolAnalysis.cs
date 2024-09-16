namespace PbDori.Model;

public class SymbolAnalysis
{
    public required string Symbol { get; set; }

    public double Volatility { get; set; }

    public double MedianVolume { get; set; }

    public double MaxLeverage { get; set; }

    public double MinQuantity { get; set; }

    public double MinNotionalValue { get; set; }

    public double BackTestLastPrice { get; set; }

    public bool CopyTradeEnabled { get; set; }
}
