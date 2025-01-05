using PbDori.Model;

namespace PbDori.Repositories;

public class InMemoryIMarketTrendResultRepository : IMarketTrendResultRepository
{
    private MarketTrend? m_marketTrend;

    public Task SaveAsync(MarketTrend marketTrend, CancellationToken cancel)
    {
        m_marketTrend = marketTrend;
        return Task.CompletedTask;
    }

    public Task<MarketTrend?> LoadAsync(CancellationToken cancel)
    {
        var marketTrend = m_marketTrend;
        return Task.FromResult(marketTrend);
    }
}