namespace PbDori.Processing;

public class SymbolQueryFilter
{
    public DateTime MinLaunchTime { get; set; }

    public DateTime Start { get; set; }

    public DateTime End { get; set; }

    public double TopDailyMedianVolumePercentile { get; set; } = 0.25;
}