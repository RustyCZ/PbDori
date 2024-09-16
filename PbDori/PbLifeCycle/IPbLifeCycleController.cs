namespace PbDori.PbLifeCycle;

public interface IPbLifeCycleController
{
    Task<bool> StartPbBackTestAsync(string configFileName, string backTestConfig, CancellationToken cancel = default);
    Task<bool> StopPbBackTestAsync(CancellationToken cancel = default);
    Task<bool> PbBackTestExitedAsync(CancellationToken cancel);
}