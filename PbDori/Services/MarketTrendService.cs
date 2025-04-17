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
                var hasExecutedWithoutErrors = true;
                try
                {
                    m_logger.LogInformation("Calculating market trend...");
                    var marketTrend = await m_marketTrendProcessor.CalculateMarketTrendAsync(cancel);
                    await m_marketTrendResultRepository.SaveAsync(marketTrend, cancel);
                    m_logger.LogInformation("Market trend calculated.");
                }
                catch (Exception e)
                {
                    m_logger.LogWarning(e, "Error in MarketTrendService.");
                    hasExecutedWithoutErrors = false;
                }

                if (hasExecutedWithoutErrors)
                {
                    m_logger.LogInformation("MarketTrendService has executed successfully.");
                    await Task.Delay(m_options.Value.Interval, cancel);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancel);
                    m_logger.LogWarning("MarketTrendService has executed with errors. Repeating...");
                }
            }
        }
        catch (OperationCanceledException)
        {
            m_logger.LogInformation("BackTestService was stopped.");
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Error in MarketTrendService.");
        }
    }
}
