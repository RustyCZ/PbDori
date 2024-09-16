using PbDori.Model;

namespace PbDori.Processing;

public interface IBackTestResultProcessor
{
    Task DeletePreviousResultsAsync(CancellationToken cancel);

    Task<BackTestResult[]> GetResultsAsync(CancellationToken cancel);
}