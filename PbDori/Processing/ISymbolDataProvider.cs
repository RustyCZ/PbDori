using PbDori.Model;

namespace PbDori.Processing;

public interface ISymbolDataProvider
{
    Task<IReadOnlyList<SymbolAnalysis>> GetSymbolsAsync(SymbolQueryFilter filter, CancellationToken cancel);
}