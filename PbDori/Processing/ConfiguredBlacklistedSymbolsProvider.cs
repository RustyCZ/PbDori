using Microsoft.Extensions.Options;

namespace PbDori.Processing;

public class ConfiguredBlacklistedSymbolsProvider : IBlacklistedSymbolsProvider
{
    private readonly IOptions<ConfiguredBlacklistedSymbolsProviderOptions> m_options;

    public ConfiguredBlacklistedSymbolsProvider(IOptions<ConfiguredBlacklistedSymbolsProviderOptions> options)
    {
        m_options = options;
    }

    public Task<string[]> GetBlacklistedSymbolsAsync(CancellationToken cancel)
    {
        return Task.FromResult(m_options.Value.BlacklistedSymbols);
    }
}