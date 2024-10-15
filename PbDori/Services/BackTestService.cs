using System.Diagnostics;
using Microsoft.Extensions.Options;
using PbDori.Helpers;
using PbDori.Model;
using PbDori.PbLifeCycle;
using PbDori.Processing;
using PbDori.Repositories;

namespace PbDori.Services;

public class BackTestService : BackgroundService
{
    private readonly IOptions<BackTestServiceOptions> m_options;
    private readonly ILogger<BackTestService> m_logger;
    private readonly IPbLifeCycleController m_pbLifeCycleController;
    private readonly IBackTestResultProcessor m_backTestResultProcessor;
    private readonly ISymbolDataProvider m_symbolDataProvider;
    private readonly IStrategyResultRepository m_strategyResultRepository;

    public BackTestService(IOptions<BackTestServiceOptions> options,
        IPbLifeCycleController pbLifeCycleController,
        IBackTestResultProcessor backTestResultProcessor,
        ISymbolDataProvider symbolDataProvider,
        ILogger<BackTestService> logger, 
        IStrategyResultRepository strategyResultRepository)
    {
        ValidateOptions(options);
        m_options = options;
        m_pbLifeCycleController = pbLifeCycleController;
        m_backTestResultProcessor = backTestResultProcessor;
        m_symbolDataProvider = symbolDataProvider;
        m_logger = logger;
        m_strategyResultRepository = strategyResultRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        try
        {
            while (!cancel.IsCancellationRequested)
            {
                var hasExecutedWithoutErrors = true;
                foreach (var strategy in m_options.Value.Strategies)
                    try
                    {
                        await BackTestStrategyAsync(strategy, cancel);
                    }
                    catch (Exception e)
                    {
                        m_logger.LogWarning(e, $"Error in BackTestService for strategy {strategy.Name}.");
                        hasExecutedWithoutErrors = false;
                    }

                if (hasExecutedWithoutErrors)
                {
                    m_logger.LogInformation("BackTestService has executed successfully.");
                    await Task.Delay(m_options.Value.Interval, cancel);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancel);
                    m_logger.LogWarning("BackTestService has executed with errors. Repeating...");
                }
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

    private async Task BackTestStrategyAsync(Strategy strategy, CancellationToken cancel)
    {
        var endTime = DateTime.UtcNow.Date;
        var symbolFilter = new SymbolQueryFilter
        {
            Start = endTime - strategy.BackTestDuration,
            End = endTime,
            MinLaunchTime = endTime - strategy.LaunchTime,
            TopDailyMedianVolumePercentile = strategy.TopDailyMedianVolumePercentile,
            EnableMarketCapFilter = strategy.EnableMarketCapFilter,
            MinMarketCapRatio = strategy.MinMarketCapRatio,
        };
        var symbolAnalysisData = await m_symbolDataProvider.GetSymbolsAsync(symbolFilter, cancel);
        var symbols = symbolAnalysisData.Select(x => x.Symbol).ToArray();
        var backtestConfig =
            BackTestConfigHelpers.GenerateBackTestConfig(symbols, symbolFilter.Start, symbolFilter.End);
        await m_backTestResultProcessor.DeletePreviousResultsAsync(cancel);
        await m_pbLifeCycleController.StopPbBackTestAsync(cancel);
        var started =
            await m_pbLifeCycleController.StartPbBackTestAsync(strategy.PbSymbolConfig, backtestConfig,
                cancel);
        var sw = Stopwatch.StartNew();
        while (!cancel.IsCancellationRequested && started)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancel);
            var hasExited = await m_pbLifeCycleController.PbBackTestExitedAsync(cancel);
            if (hasExited)
            {
                await m_pbLifeCycleController.StopPbBackTestAsync(cancel);
                break;
            }

            if (sw.Elapsed > m_options.Value.MaxExecutionDuration)
            {
                await m_pbLifeCycleController.StopPbBackTestAsync(cancel);
                throw new TimeoutException("BackTestService has exceeded MaxExecutionDuration.");
            }
        }

        var results = await m_backTestResultProcessor.GetResultsAsync(cancel);
        if (results.Length != symbols.Length)
            throw new InvalidOperationException("Results count does not match symbols count.");
        var symbolAnalysisDict = symbolAnalysisData.ToDictionary(x => x.Symbol);
        var symbolPerformance = results
            .Select(x =>
            {
                if (x.Result.Symbol == null)
                    throw new InvalidOperationException("Symbol is null.");
                var symbolAnalysis = symbolAnalysisDict[x.Result.Symbol];
                var performance = new SymbolPerformance
                {
                    Symbol = x.Result.Symbol,
                    Volatility = symbolAnalysis.Volatility,
                    MedianVolume = symbolAnalysis.MedianVolume,
                    MaxLeverage = symbolAnalysis.MaxLeverage,
                    MinNotionalValue = symbolAnalysis.MinNotionalValue,
                    BackTestLastPrice = symbolAnalysis.BackTestLastPrice,
                    MinQuantity = symbolAnalysis.MinQuantity,
                    CopyTradeEnabled = symbolAnalysis.CopyTradeEnabled,
                    BackTestResult = x,
                };
                return performance;
            })
            .ToArray();
        await m_strategyResultRepository.SaveAsync(strategy.Name, symbolPerformance, cancel);
    }

    private void ValidateOptions(IOptions<BackTestServiceOptions> options)
    {
        foreach (var strategy in options.Value.Strategies)
        {
            if (strategy.BackTestDuration <= TimeSpan.Zero)
                throw new ArgumentException("BackTestDuration must be greater than zero.");
            if (strategy.LaunchTime <= TimeSpan.Zero)
                throw new ArgumentException("LaunchTime must be greater than zero.");
            if (strategy.TopDailyMedianVolumePercentile < 0 || strategy.TopDailyMedianVolumePercentile > 1)
                throw new ArgumentException("TopDailyMedianVolumePercentile must be between 0 and 1.");
            if (strategy.BackTestDuration > strategy.LaunchTime)
                throw new ArgumentException("BackTestDuration must be less than LaunchTime.");
            if (string.IsNullOrWhiteSpace(strategy.PbSymbolConfig))
                throw new ArgumentException("PbSymbolConfig must not be empty.");
        }
    }
}