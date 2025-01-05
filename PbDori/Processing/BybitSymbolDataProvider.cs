using System.Text.RegularExpressions;
using Bybit.Net.Enums;
using Bybit.Net.Interfaces.Clients;
using Bybit.Net.Objects.Models.V5;
using PbDori.CoinMarketCap;
using PbDori.Model;

namespace PbDori.Processing;

public class BybitSymbolDataProvider : ISymbolDataProvider
{
    private readonly IBybitRestClient m_bybitRestClient;
    private readonly ICoinMarketCapClient m_coinMarketCapClient;
    private readonly IBlacklistedSymbolsProvider m_blacklistedSymbolsProvider;

    public BybitSymbolDataProvider(IBybitRestClient bybitRestClient, 
        ICoinMarketCapClient coinMarketCapClient, 
        IBlacklistedSymbolsProvider blacklistedSymbolsProvider)
    {
        m_bybitRestClient = bybitRestClient;
        m_coinMarketCapClient = coinMarketCapClient;
        m_blacklistedSymbolsProvider = blacklistedSymbolsProvider;
    }

    public async Task<IReadOnlyList<SymbolAnalysis>> GetSymbolsAsync(SymbolQueryFilter filter, CancellationToken cancel)
    {
        var blacklist = await m_blacklistedSymbolsProvider.GetBlacklistedSymbolsAsync(cancel);
        var blacklistSet = blacklist
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(SymbolHelpers.NormalizeCoin).ToHashSet();
        var delistings = await QueryDelistingsAsync(cancel);
        var symbolsData = new List<SymbolAnalysis>();
        string? cursor = null;
        var symbols = new List<BybitLinearInverseSymbol>();
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
        var tickersBySymbol = tickerRes.Data.List.ToDictionary(x => x.Symbol);
        MarketData? marketData = null;
        if (filter.EnableMarketCapFilter)
        {
            marketData = await m_coinMarketCapClient.GetMarketDataAsync(cancel);
            if (marketData == null)
                throw new InvalidOperationException("Failed to get market data");
        }
            
        while (true)
        {
            var symbolsRes = await m_bybitRestClient.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, null, null,
                SymbolStatus.Trading, 1000, cursor, cancel);
            if (symbolsRes.Error != null)
                throw new InvalidOperationException($"Failed to get symbols: {symbolsRes.Error.Message}");
            if (symbolsRes.Data == null)
                throw new InvalidOperationException("Failed to get symbols: no data returned");
            foreach (var symbol in symbolsRes.Data.List)
            {
                if (symbol.Status != SymbolStatus.Trading)
                    continue;
                if (!SymbolHelpers.IsTradedQuotedAsset(symbol.QuoteAsset))
                    continue;
                if (SymbolHelpers.IsStableCoinPair(symbol.Name))
                    continue;
                if (symbol.LaunchTime > filter.MinLaunchTime)
                    continue;
                if (symbol.LeverageFilter == null)
                    continue;
                if (symbol.LotSizeFilter == null)
                    continue;
                if (!symbol.LotSizeFilter.MinNotionalValue.HasValue)
                    continue;
                if (delistings.Contains(symbol.Name))
                    continue;
                if (filter.EnableMarketCapFilter && IsFilteredByNotice(marketData, symbol.Name))
                    continue;
                if (IsBlackListed(symbol.Name, blacklistSet))
                    continue;
                symbols.Add(symbol);
            }
            if (string.IsNullOrWhiteSpace(symbolsRes.Data.NextPageCursor))
                break;
            cursor = symbolsRes.Data.NextPageCursor;
        }

        foreach (var bybitLinearInverseSymbol in symbols)
        {
            var ticker = tickersBySymbol[bybitLinearInverseSymbol.Name];
            var symbolData = await CalculateVolatilityBybitAsync(filter.Start, filter.End, bybitLinearInverseSymbol, ticker);
            if (symbolData.Volatility > 0)
                symbolsData.Add(symbolData);
            await Task.Delay(100, cancel);
        }

        int percentileCount = (int)(symbolsData.Count * filter.TopDailyMedianVolumePercentile);
        if (percentileCount == 0)
            percentileCount = 1;
        symbolsData = symbolsData
            .OrderByDescending(x => x.MedianVolume)
            .Take(percentileCount)
            .Where(x => !IsFilteredByMarketCap(marketData, x.Symbol, filter))
            .ToList();

        return symbolsData;
    }

    private bool IsFilteredByMarketCap(MarketData? marketData, string symbol, SymbolQueryFilter filter)
    {
        if (!filter.EnableMarketCapFilter)
            return false;
        string normalizedCoin = SymbolHelpers.NormalizeCoin(symbol);
        if (!marketData!.MarketCapRatioBySymbol.TryGetValue(normalizedCoin, out var marketCapRatio))
            return true;
        return marketCapRatio < filter.MinMarketCapRatio;
    }

    private bool IsFilteredByNotice(MarketData? marketData, string symbol)
    {
        string normalizedCoin = SymbolHelpers.NormalizeCoin(symbol);
        if (!marketData!.NoticeBySymbol.TryGetValue(normalizedCoin, out var notice))
            return false; // do not filter if notice is not found
        return !string.IsNullOrWhiteSpace(notice);
    }

    private bool IsBlackListed(string symbol, HashSet<string> blacklist)
    {
        string normalizedCoin = SymbolHelpers.NormalizeCoin(symbol);
        bool blacklisted = blacklist.Contains(normalizedCoin);
        return blacklisted;
    }

    private async Task<HashSet<string>> QueryDelistingsAsync(CancellationToken cancel)
    {
        var announcements = await m_bybitRestClient.V5Api.ExchangeData.GetAnnouncementsAsync("en-US", "delistings", null, null, 100, cancel);
        if (announcements.Error != null)
            throw new InvalidOperationException($"Failed to get announcements: {announcements.Error.Message}");
        List<Regex> patterns =
        [
            new Regex("^Delisting of (?<symbol>.+)USDT Perpetual Contract$"),
            new Regex("^Delisting of (?<symbol>.+) Perpetual Contracts$")
        ];
        HashSet<string> delistings = new HashSet<string>();
        TimeSpan delistingValidityDuration = TimeSpan.FromDays(60);
        DateTime delistingExpiration = DateTime.UtcNow - delistingValidityDuration;
        List<string> normalizedTitles = new List<string>();
        foreach (var announcement in announcements.Data.List)
        {
            if (announcement.Timestamp < delistingExpiration)
                continue;
            if (string.IsNullOrWhiteSpace(announcement.Title))
                continue;
            // replace white space with single space
            var normalizedTitle = Regex.Replace(announcement.Title, @"\s+", " ");
            normalizedTitles.Add(normalizedTitle);
        }
        foreach (var pattern in patterns)
        {
            var matches = normalizedTitles
                .Select(x => pattern.Match(x))
                .Where(x => x.Success);
            foreach (Match match in matches)
            {
                var symbol = match.Groups["symbol"].Value;
                symbol = symbol.Replace("and", ",");
                symbol = symbol.Replace(" ", "");
                var symbols = symbol.Split(',');
                foreach (var s in symbols)
                {
                    var trimmed = s.Trim();
                    if(string.IsNullOrWhiteSpace(trimmed))
                        continue;
                    if (!trimmed.EndsWith("USDT"))
                        trimmed += "USDT";
                    delistings.Add(trimmed);
                }
            }
        }

        return delistings;
    }

    private async Task<SymbolAnalysis> CalculateVolatilityBybitAsync(DateTime start, DateTime end, BybitLinearInverseSymbol symbol, BybitLinearInverseTicker ticker)
    {
        IBybitRestClient client = m_bybitRestClient;
        var klines = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Linear,
            symbol.Name,
            KlineInterval.OneDay,
            start,
            end,
            1000);
        var klinesArr = klines.Data.List.ToArray();
        var medianVolume = klinesArr.Select(x => x.QuoteVolume).OrderBy(x => x).ElementAt(klinesArr.Length / 2);
        var volatility = klinesArr
            .Select(x => (x.HighPrice - x.LowPrice) / Math.Abs(x.LowPrice) * 100 / klinesArr.Length).Sum();
        var symbolData = new SymbolAnalysis
        {
            Symbol = symbol.Name,
            Volatility = (double)volatility,
            MedianVolume = (double)medianVolume,
            MaxLeverage = (double)symbol.LeverageFilter!.MaxLeverage,
            MinQuantity = (double)symbol.LotSizeFilter!.MinOrderQuantity,
            MinNotionalValue = (double)symbol.LotSizeFilter!.MinNotionalValue!.Value,
            BackTestLastPrice = (double)ticker.LastPrice,
            CopyTradeEnabled = symbol.CopyTrading == CopyTradeType.Both,
        };
        return symbolData;
    }
}