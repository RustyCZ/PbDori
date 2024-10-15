namespace PbDori.CoinMarketCap;

public interface ICoinMarketCapClient
{
    Task<MarketData?> GetMarketDataAsync(CancellationToken cancel = default);
}