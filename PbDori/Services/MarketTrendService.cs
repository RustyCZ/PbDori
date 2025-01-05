using PbDori.Processing;
using Microsoft.Extensions.Options;
using PbDori.Repositories;

namespace PbDori.Services;

public class MarketTrendService : BackgroundService
{
    private readonly IOptions<MarketTrendServiceOptions> m_options;
    private readonly IMarketTrendProcessor m_marketTrendProcessor;
    private readonly IMarketTrendResultRepository m_marketTrendResultRepository;
    private readonly ILogger<BackTestService> m_logger;

    public MarketTrendService(IOptions<MarketTrendServiceOptions> options,
        IMarketTrendProcessor marketTrendProcessor,
        IMarketTrendResultRepository marketTrendResultRepository,
        ILogger<BackTestService> logger)
    {
        m_options = options;
        m_marketTrendProcessor = marketTrendProcessor;
        m_marketTrendResultRepository = marketTrendResultRepository;
        m_logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                m_logger.LogInformation("Calculating market trend...");
                var marketTrend = await m_marketTrendProcessor.CalculateMarketTrendAsync(cancel);
                await m_marketTrendResultRepository.SaveAsync(marketTrend, cancel);
                m_logger.LogInformation("Market trend calculated.");
                await Task.Delay(m_options.Value.Interval, cancel);
            }
        }
        catch (OperationCanceledException)
        {
            m_logger.LogInformation("BackTestService was stopped.");
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Error in BackTestService.");
        }
    }
}
