using PbDori.Model;

namespace PbDori.Repositories;

public class StrategyResult
{
    public string Name { get; set; } = string.Empty;

    public SymbolPerformance[] SymbolData { get; set; } = [];
}