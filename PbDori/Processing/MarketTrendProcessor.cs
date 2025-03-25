using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using PbDori.CoinMarketCap;
using PbDori.Model;
using Skender.Stock.Indicators;

namespace PbDori.Processing;

public class MarketTrendProcessor : IMarketTrendProcessor
{
    private const int TrendMinCount = 100;
    private readonly IBybitRestClient m_bybitRestClient;
    private readonly ICoinMarketCapClient m_coinMarketCapClient;
    private readonly record struct CoinToSymbol(string Coin, string Symbol);

    public MarketTrendProcessor(IBybitRestClient bybitRestClient, ICoinMarketCapClient coinMarketCapClient)
    {
        m_bybitRestClient = bybitRestClient;
        m_coinMarketCapClient = coinMarketCapClient;
    }

    public async Task<MarketTrend> CalculateMarketTrendAsync(CancellationToken cancel = default)
    {
        var tickerRes = await m_bybitRestClient.V5Api.ExchangeData.GetLinearInverseTickersAsync(
            Category.Linear,
            null,
            null,
            null,
            CancellationToken.None);
        if (tickerRes.Error != null)
            throw new InvalidOperationException($"Failed to get tickers: {tickerRes.Error.Message}");
        if (tickerRes.Data == null)
            throw new InvalidOperationException("Failed to get tickers: no data returned");
        var bybitSymbols = tickerRes.Data.List
            .Where(x => SymbolHelpers.IsTradedSymbol(x.Symbol))
            .Select(x => new CoinToSymbol(SymbolHelpers.NormalizeCoin(x.Symbol), x.Symbol))
            .DistinctBy(x => x.Coin)
            .ToDictionary(x => SymbolHelpers.NormalizeCoin(x.Symbol), x => x.Symbol);
        var coinMarketCapData = await m_coinMarketCapClient.GetMarketDataAsync(cancel);
        if (coinMarketCapData == null)
        {
            return new MarketTrend
            {
                LastUpdated = DateTime.UtcNow,
                GlobalTrend = Trend.Unknown,
            };
        }
            
        var trendBySymbol = new Dictionary<string, Trend>();
        DateTime end = DateTime.UtcNow;
        const int minCount = 50;
        const int optimalCount = 500; // this should be enough for ema
        const int minHours = 4 * optimalCount;
        DateTime start = end.AddHours(-minHours);
        var marketCapOrdered = coinMarketCapData.MarketCapBySymbol
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToList();
        foreach (var symbol in marketCapOrdered)
        {
            if (!bybitSymbols.TryGetValue(symbol, out var bybitSymbol))
                continue;
            if (SymbolHelpers.IsStableCoinPair(bybitSymbol))
                continue;
            var trend = await CalculateSymbolTrendAsync(bybitSymbol, start, end, minCount, cancel);
            trendBySymbol[bybitSymbol] = trend;
            var hasTrendCount = trendBySymbol.Count(x => x.Value != Trend.Unknown);
            if (hasTrendCount >= TrendMinCount)
                break;
            await Task.Delay(1000, cancel);
        }
        var bullishCount = trendBySymbol.Count(x => x.Value == Trend.Bullish);
        var bearishCount = trendBySymbol.Count(x => x.Value == Trend.Bearish);
        var unknownCount = trendBySymbol.Count(x => x.Value == Trend.Unknown);
        var globalTrend = bullishCount > bearishCount ? Trend.Bullish : Trend.Bearish;
        var marketTrend = new MarketTrend
        {
            LastUpdated = DateTime.UtcNow,
            GlobalTrend = globalTrend,
            BullishCount = bullishCount,
            BearishCount = bearishCount,
            UnknownCount = unknownCount,
            SymbolTrends = trendBySymbol.Select(x => new SymbolTrend
                {
                    Symbol = x.Key,
                    Trend = x.Value,
                })
                .ToArray(),
        };

        return marketTrend;
    }

    private async Task<Trend> CalculateSymbolTrendAsync(string symbol, DateTime start, DateTime end, int minCount, CancellationToken cancel)
    {
        var klines = await m_bybitRestClient.V5Api.ExchangeData.GetKlinesAsync(Category.Linear,
            symbol,
            KlineInterval.FourHours,
            start,
            end,
            1000,
            cancel);
        if (klines.Error != null)
            throw new InvalidOperationException($"Failed to get klines for {symbol}: {klines.Error.Message}");
        if (klines.Data == null)
            throw new InvalidOperationException($"Failed to get klines for {symbol}: no data returned");
        var closes = klines.Data.List
            .Select(x => (x.StartTime, (double)x.ClosePrice))
            .Reverse()
            .ToList();
        if (closes.Count < minCount)
            return Trend.Unknown;
        var macd = closes.GetMacd();
        var lastMacd = macd.LastOrDefault();
        if (lastMacd == null)
            return Trend.Unknown;
        var trend = lastMacd.Macd > lastMacd.Signal ? Trend.Bullish : Trend.Bearish;
        return trend;
    }
}