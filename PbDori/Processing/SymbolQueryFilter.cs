namespace PbDori.Processing;

public class SymbolQueryFilter
{
    public DateTime MinLaunchTime { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public double TopDailyMedianVolumePercentile { get; set; } = 0.25;

    public bool EnableMarketCapFilter { get; set; }

    public double MinMarketCapRatio { get; set; } = 0.0003;
}