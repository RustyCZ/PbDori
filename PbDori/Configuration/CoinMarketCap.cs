namespace PbDori.Configuration;

public class CoinMarketCap
{
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(8);
    public int CoinLimit { get; set; } = 200;
}