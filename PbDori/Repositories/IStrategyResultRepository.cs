using PbDori.Model;

namespace PbDori.Repositories;

public interface IStrategyResultRepository
{
    Task SaveAsync(string strategyName, SymbolPerformance[] symbolData, CancellationToken cancel);

    Task<SymbolPerformance[]?> LoadAsync(string strategyName, CancellationToken cancel);
}