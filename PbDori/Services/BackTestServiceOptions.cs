namespace PbDori.Services;

public class BackTestServiceOptions
{
    public Strategy[] Strategies { get; set; } = [];

    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(2);

    public TimeSpan MaxExecutionDuration { get; set; } = TimeSpan.FromHours(3);
}