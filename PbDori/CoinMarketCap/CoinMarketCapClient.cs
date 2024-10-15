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
                var marketCapBySymbolTask = GetMarketCapBySymbolAsync(m_options.Value.CoinLimit, cancel);
                await Task.WhenAll(totalMarketCapTask, marketCapBySymbolTask);
                var marketCapBySymbol = marketCapBySymbolTask.Result;
                var totalMarketCap = totalMarketCapTask.Result;
                if (totalMarketCap == null || marketCapBySymbol.Count == 0)
                {
                    m_logger.LogWarning("Failed to get market data");
                    return m_marketData;
                }
                m_marketData = new MarketData
                {
                    TotalMarketCap = totalMarketCapTask.Result,
                    MarketCapBySymbol = marketCapBySymbol,
                    LastUpdated = DateTime.UtcNow,
                    MarketCapRatioBySymbol = marketCapBySymbol.ToDictionary(x => x.Key, x => x.Value / totalMarketCap.Value)
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

    public async Task<Dictionary<string, double>> GetMarketCapBySymbolAsync(int limit, CancellationToken cancel = default)
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
            return new Dictionary<string, double>();
        var marketCapBySymbol = listingsResult.Data
            .Where(x => x.Quote?.Usd?.MarketCap != null && !string.IsNullOrWhiteSpace(x.Symbol))
            .DistinctBy(x => x.Symbol!)
            .ToDictionary(x => x.Symbol!, x => x.Quote!.Usd!.MarketCap!.Value);
        return marketCapBySymbol;
    }
}