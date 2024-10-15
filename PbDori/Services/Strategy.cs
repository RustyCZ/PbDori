namespace PbDori.Services;

public class Strategy
{
    public string Name { get; set; } = string.Empty;

    public TimeSpan BackTestDuration { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan LaunchTime { get; set; } = TimeSpan.FromDays(90);

    public double TopDailyMedianVolumePercentile { get; set; } = 0.15;

    public string PbSymbolConfig { get; set; } = string.Empty;

    public bool EnableMarketCapFilter { get; set; }

    public double MinMarketCapRatio { get; set; } = 0.0003;
}