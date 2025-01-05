using PbDori.Model;

namespace PbDori.Repositories;

public interface IMarketTrendResultRepository
{
    Task SaveAsync(MarketTrend marketTrend, CancellationToken cancel);
    Task<MarketTrend?> LoadAsync(CancellationToken cancel);
}