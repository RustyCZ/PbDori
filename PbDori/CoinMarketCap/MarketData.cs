namespace PbDori.CoinMarketCap;

public class MarketData
{
    public double? TotalMarketCap { get; set; }

    public required Dictionary<string, double> MarketCapBySymbol { get; set; }

    /// <summary>
    /// Gets the market cap ratio of each coin to the total market cap.
    /// </summary>
    public required Dictionary<string, double> MarketCapRatioBySymbol { get; set; }

    public DateTime LastUpdated { get; set; }
}