using PbDori.Model;

namespace PbDori.Processing;

public interface IMarketTrendProcessor
{
    Task<MarketTrend> CalculateMarketTrendAsync(CancellationToken cancel = default);
}