using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PbDori.Model;

namespace PbDori.Processing;

public class BackTestResultProcessor : IBackTestResultProcessor
{
    private const string PlotsSubDir = "plots";
    private const string ResultFileName = "result.json";
    private readonly IOptions<BackTestResultProcessorOptions> m_options;

    public BackTestResultProcessor(IOptions<BackTestResultProcessorOptions> options)
    {
        m_options = options;
    }

    public Task DeletePreviousResultsAsync(CancellationToken cancel)
    {
        var basePath = m_options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(basePath))
            return Task.CompletedTask;
        if (!Directory.Exists(basePath))
            return Task.CompletedTask;
        var directories = Directory.GetDirectories(basePath);
        foreach (var directory in directories)
        {
            if (cancel.IsCancellationRequested)
                break;
            var subDirectories = Directory.GetDirectories(directory);
            foreach (var subDirectory in subDirectories)
            {
                if (cancel.IsCancellationRequested)
                    break;
                string directoryName = Path.GetFileName(subDirectory);
                if (!string.Equals(directoryName, PlotsSubDir))
                    continue;
                Directory.Delete(subDirectory, true);
            }
        }

        return Task.CompletedTask;
    }

    public async Task<BackTestResult[]> GetResultsAsync(CancellationToken cancel)
    {
        var results = new List<BackTestResult>();
        var basePath = m_options.Value.BasePath;
        if (string.IsNullOrWhiteSpace(basePath))
            return [];
        if (!Directory.Exists(basePath))
            return [];
        var directories = Directory.GetDirectories(basePath);
        foreach (var directory in directories)
        {
            if (cancel.IsCancellationRequested)
                break;
            var subDirectories = Directory.GetDirectories(directory);
            foreach (var subDirectory in subDirectories)
            {
                if (cancel.IsCancellationRequested)
                    break;
                string directoryName = Path.GetFileName(subDirectory);
                if (!string.Equals(directoryName, PlotsSubDir))
                    continue;
                var plotDirs = Directory.GetDirectories(subDirectory);
                var lastPlotDir = plotDirs.LastOrDefault();
                if (lastPlotDir == null)
                    continue;
                var resultFile = Path.Combine(lastPlotDir, ResultFileName);
                if (!File.Exists(resultFile))
                    continue;
                var resultJson = await File.ReadAllTextAsync(resultFile, cancel);
                string[] lines = resultJson.Split('\n');
                var sb = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.EndsWith("NaN,"))
                        continue;
                    sb.AppendLine(line);
                }
                resultJson = sb.ToString();
                var result = JsonSerializer.Deserialize<BackTestResult>(resultJson);
                if (result != null)
                    results.Add(result);
            }
        }

        return results.ToArray();
    }
}
