using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace PbDori.CoinMarketCap;

public class CoinMarketCapClient : ICoinMarketCapClient
{
    private readonly IOptions<CoinMarketCapClientOptions> m_options;
    private readonly HttpClient m_client;
    private readonly ILogger<CoinMarketCapClient> m_logger;
    private const string ApiKeyHeader = "X-CMC_PRO_API_KEY";
    private readonly AsyncLock m_lock;
    private Stopwatch? m_lastUpdate;
    private MarketData? m_marketData;

    public CoinMarketCapClient(IOptions<CoinMarketCapClientOptions> options, HttpClient client, ILogger<CoinMarketCapClient> logger)
    {
        m_lock = new AsyncLock();
        m_options = options;
        m_client = client;
        m_logger = logger;
    }

    public async Task<MarketData?> GetMarketDataAsync(CancellationToken cancel = default)
    {
        string apiKey = m_options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            m_logger.LogWarning("API key is not set");
            return m_marketData;
        }
        using (await m_lock.LockAsync(cancel))
        {
            if (m_marketData != null && m_lastUpdate != null && m_lastUpdate.Elapsed < m_options.Value.CacheDuration)
                return m_marketData;

            try
            {
                m_logger.LogInformation("Getting market data");
                var totalMarketCapTask = GetTotalMarketCapAsync(cancel);
                var symbolInfoTask = GetSymbolInfoAsync(m_options.Value.CoinLimit, cancel);
                await Task.WhenAll(totalMarketCapTask, symbolInfoTask);
                var symbolInfo = symbolInfoTask.Result;
                var totalMarketCap = totalMarketCapTask.Result;
                if (totalMarketCap == null || symbolInfo.Count == 0)
                {
                    m_logger.LogWarning("Failed to get market data");
                    return m_marketData;
                }

                var marketCapBySymbol = symbolInfo.Values.ToDictionary(x => x.Symbol, x => x.MarketCap);
                m_marketData = new MarketData
                {
                    TotalMarketCap = totalMarketCapTask.Result,
                    MarketCapBySymbol = marketCapBySymbol,
                    LastUpdated = DateTime.UtcNow,
                    MarketCapRatioBySymbol = marketCapBySymbol.ToDictionary(x => x.Key, x => x.Value / totalMarketCap.Value),
                    NoticeBySymbol = symbolInfo.ToDictionary(x => x.Key, x => x.Value.Notice)
                };
                m_logger.LogInformation("Market data updated");
                m_logger.LogInformation("Total market cap: {TotalMarketCap}", m_marketData.TotalMarketCap);
                m_logger.LogInformation("Number of coins: {CoinCount}", m_marketData.MarketCapBySymbol.Count);
                m_lastUpdate = Stopwatch.StartNew();
                return m_marketData;
            }
            catch (Exception e)
            {
                m_logger.LogWarning(e, "Failed to get market data");
                return m_marketData;
            }
        }
    }

    public async Task<double?> GetTotalMarketCapAsync(CancellationToken cancel = default)
    {
        const string globalMetrics = "https://pro-api.coinmarketcap.com/v1/global-metrics/quotes/latest";
        var request = new HttpRequestMessage(HttpMethod.Get, globalMetrics);
        request.Headers.Add(ApiKeyHeader, m_options.Value.ApiKey);
        var response = await m_client.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancel);
        var globalMetricsResult = JsonSerializer.Deserialize<GlobalMetricsResult>(result);

        return globalMetricsResult?.Data?.Quote?.Usd?.TotalMarketCap;
    }

    public async Task<Dictionary<string, SymbolInfo>> GetSymbolInfoAsync(int limit, CancellationToken cancel = default)
    {
        const string listingUrl = "https://pro-api.coinmarketcap.com/v1/cryptocurrency/listings/latest";
        const string sort = "market_cap";
        const string sortDir = "desc";
        var url = $"{listingUrl}?limit={limit}&sort={sort}&sort_dir={sortDir}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(ApiKeyHeader, m_options.Value.ApiKey);
        var response = await m_client.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync(cancel);
        var listingsResult = JsonSerializer.Deserialize<ListingsResult>(result);
        if (listingsResult?.Data == null)
            return new Dictionary<string, SymbolInfo>();
        var marketCapBySymbol = listingsResult.Data
            .Where(x => x.Quote?.Usd?.MarketCap != null && !string.IsNullOrWhiteSpace(x.Symbol))
            .DistinctBy(x => x.Symbol!)
            .ToDictionary(x => x.Symbol!, x => new SymbolInfo(x.Symbol!, x.Quote!.Usd!.MarketCap!.Value, string.Empty));

        const string metaDataUrl = "https://pro-api.coinmarketcap.com/v2/cryptocurrency/info";
        string ids = string.Join(',', listingsResult.Data.Select(x => x.Id));
        url = $"{metaDataUrl}?id={ids}";
        request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add(ApiKeyHeader, m_options.Value.ApiKey);
        response = await m_client.SendAsync(request, cancel);
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadAsStringAsync(cancel);
        CryptoCurrencyInfoResult? cryptoCurrencyInfoResult = JsonSerializer.Deserialize<CryptoCurrencyInfoResult>(result);
        if (cryptoCurrencyInfoResult?.Data == null)
            return marketCapBySymbol;
        foreach (var (_, info) in cryptoCurrencyInfoResult.Data)
        {
            if (marketCapBySymbol.TryGetValue(info.Symbol, out var symbolInfo))
                marketCapBySymbol[info.Symbol] = new SymbolInfo(info.Symbol, symbolInfo.MarketCap, info.Notice);
        }
        return marketCapBySymbol;
    }
}