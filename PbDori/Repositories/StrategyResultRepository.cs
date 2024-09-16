using System.Text.Json;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using PbDori.Model;

namespace PbDori.Repositories;

public class StrategyResultRepository : IStrategyResultRepository
{
    private readonly IOptions<StrategyResultRepositoryOptions> m_options;
    private readonly ILogger<StrategyResultRepository> m_logger;
    private readonly AsyncLock m_lock;
    private readonly Dictionary<string, SymbolPerformance[]> m_data;
    private bool m_loaded;

    public StrategyResultRepository(IOptions<StrategyResultRepositoryOptions> options, 
        ILogger<StrategyResultRepository> logger)
    {
        m_options = options;
        m_logger = logger;
        m_lock = new AsyncLock();
        m_data = new Dictionary<string, SymbolPerformance[]>();
    }

    public async Task SaveAsync(string strategyName, SymbolPerformance[] symbolData, CancellationToken cancel)
    {
        using (await m_lock.LockAsync(cancel))
        {
            m_data[strategyName] = symbolData;
            await SaveToFileAsync(cancel);
        }
    }

    public async Task<SymbolPerformance[]?> LoadAsync(string strategyName, CancellationToken cancel)
    {
        using (await m_lock.LockAsync(cancel))
        {
            await LoadFromFileAsync(cancel);
            var result = m_data.GetValueOrDefault(strategyName);
            return result;
        }
    }

    private async Task SaveToFileAsync(CancellationToken cancel)
    {
        var resultsFile = m_options.Value.ResultsFile;
        if(string.IsNullOrWhiteSpace(resultsFile))
            return;

        var data = new StrategyResultData
        {
            Strategies = m_data.Select(x => new StrategyResult
            {
                Name = x.Key,
                SymbolData = x.Value
            }).ToArray()
        };

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(data, jsonOptions);
        var directory = Path.GetDirectoryName(resultsFile);
        if(directory != null && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(m_options.Value.ResultsFile, json, cancel);
    }

    private async Task LoadFromFileAsync(CancellationToken cancel)
    {
        if(m_loaded)
            return;
        try
        {
            if (!File.Exists(m_options.Value.ResultsFile))
                return;
            var json = await File.ReadAllTextAsync(m_options.Value.ResultsFile, cancel);
            var data = JsonSerializer.Deserialize<StrategyResultData>(json);
            if (data == null)
                return;
            foreach (var strategy in data.Strategies)
                m_data[strategy.Name] = strategy.SymbolData;
            m_loaded = true;
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Failed to load back test results from file");
        }
    }
}