namespace PbDori.Processing;

public interface IBlacklistedSymbolsProvider
{
    Task<string[]> GetBlacklistedSymbolsAsync(CancellationToken cancel);
}