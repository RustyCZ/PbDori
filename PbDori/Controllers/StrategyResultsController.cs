using Microsoft.AspNetCore.Mvc;
using PbDori.Model;
using PbDori.Repositories;

namespace PbDori.Controllers;
[ApiController]
[Route("[controller]")]
public class StrategyResultsController : ControllerBase
{
    private readonly IStrategyResultRepository m_strategyResultRepository;

    public StrategyResultsController(IStrategyResultRepository strategyResultRepository)
    {
        m_strategyResultRepository = strategyResultRepository;
    }

    [HttpGet(Name = "GetStrategyResults")]
    public async Task<StrategyApiResult> Get(string? strategyName, 
        int maxSymbolCount, 
        double minAllowedExchangeLeverage, 
        double initialOrderSize, 
        bool filterCopyTradeEnabled,
        string? ignoredSymbols)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
            return new StrategyApiResult { DataAvailable = false };
        var result = await m_strategyResultRepository.LoadAsync(strategyName, HttpContext.RequestAborted);
        var filteredResult = Array.Empty<SymbolPerformance>();
        double totalLongAdg = 0;
        HashSet<string> ignoredSymbolSet = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        if (!string.IsNullOrWhiteSpace(ignoredSymbols))
        {
            string[] symbols = ignoredSymbols.Split(',');
            foreach (var symbol in symbols)
                ignoredSymbolSet.Add(symbol);
        }
            
        if (result != null)
        {
            var prefilteredResult = result.AsEnumerable();
            if (filterCopyTradeEnabled)
                prefilteredResult = prefilteredResult.Where(x => x.CopyTradeEnabled);
            filteredResult = prefilteredResult
                .Where(x => x.MaxLeverage >= minAllowedExchangeLeverage 
                            && !ignoredSymbolSet.Contains(x.Symbol)
                            && (x.MinQuantity * x.BackTestLastPrice) <= initialOrderSize 
                            && x.BackTestResult.Result.AdgLong > 0)
                .OrderByDescending(x => x.BackTestResult.Result.AdgLong)
                .Take(maxSymbolCount)
                .ToArray();
            if (filteredResult.Any())
                totalLongAdg = filteredResult.Sum(x => x.BackTestResult.Result.AdgLong!.Value);
        }
        return new StrategyApiResult
        {
            StrategyName = strategyName,
            SymbolData = filteredResult,
            TotalLongAdg = totalLongAdg,
            DataAvailable = result != null
        };
    }
}
