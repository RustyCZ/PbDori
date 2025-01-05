namespace PbDori.Services;

public class MarketTrendServiceOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}