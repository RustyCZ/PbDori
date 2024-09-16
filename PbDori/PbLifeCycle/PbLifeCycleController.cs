using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;

namespace PbDori.PbLifeCycle;

public class PbLifeCycleController : IPbLifeCycleController
{
    private readonly IOptions<PbLifeCycleControllerOptions> m_options;
    private readonly ILogger<PbLifeCycleController> m_logger;
    private const string PbDoriLabel = "pbdori";

    public PbLifeCycleController(IOptions<PbLifeCycleControllerOptions> options, ILogger<PbLifeCycleController> logger)
    {
        m_options = options;
        m_logger = logger;
    }

    public async Task<bool> StartPbBackTestAsync(string configFileName, string backTestConfig, CancellationToken cancel = default)
    {
        try
        {
            using DockerClient client = new DockerClientConfiguration(
                    new Uri(m_options.Value.DockerHost))
                .CreateClient();
            await StopPbBackTestAsync(cancel);
            if (m_options.Value.BackTestConfigPathHost != null)
                await File.WriteAllTextAsync(m_options.Value.BackTestConfigPathHost, backTestConfig, cancel);
            var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = m_options.Value.Image,
                Name = "passivbot_backtest",
                Labels = new Dictionary<string, string>()
                {
                    { PbDoriLabel, PbDoriLabel },
                },
                HostConfig = new HostConfig()
                {
                    Binds = new List<string>()
                    {
                        FormattableString.Invariant($"{m_options.Value.MountConfigsPath}:{m_options.Value.ConfigsPath}"),
                        FormattableString.Invariant($"{m_options.Value.MountApiKeysPath}:{m_options.Value.ApiKeysPath}"),
                        FormattableString.Invariant($"{m_options.Value.MountBackTestsPath}:{m_options.Value.BackTestsPath}"),
                        FormattableString.Invariant($"{m_options.Value.MountHistoricalDataPath}:{m_options.Value.HistoricalDataPath}"),
                    },
                    RestartPolicy = new RestartPolicy
                    {
                        Name = RestartPolicyKind.No,
                    },
                },
                Cmd = new List<string>()
                {
                    "python",
                    "-u",
                    "backtest.py",
                    "--disable_plotting",
                    FormattableString.Invariant($"configs/{configFileName}")
                },
            }, cancel);
            bool started = await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters(), cancel);
            if (started)
                m_logger.LogInformation("Started container '{ContainerId}' with backtest config '{Config}' ", response.ID, configFileName);
            else
                m_logger.LogError("Failed to start container '{ContainerId}' with backtest config '{Config}'", response.ID, configFileName);
            return started;
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Failed to start passivbot with backtest config '{Config}'", configFileName);
            return false;
        }
    }

    public async Task<bool> StopPbBackTestAsync(CancellationToken cancel = default)
    {
        try
        {
            using DockerClient client = new DockerClientConfiguration(
                    new Uri(m_options.Value.DockerHost))
                .CreateClient();
            var containerIds = await FindPDoriContainers(client, cancel);
            foreach (var containerId in containerIds)
                await StopAndRemovePbAsync(client, containerId, cancel);
            return true;
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Failed to stop and remove containers");
            return false;
        }
    }

    public async Task<bool> PbBackTestExitedAsync(CancellationToken cancel)
    {
        try
        {
            using DockerClient client = new DockerClientConfiguration(
                    new Uri(m_options.Value.DockerHost))
                .CreateClient();
            var containerIds = await FindPDoriContainers(client, cancel);
            if(containerIds.Length == 0)
                return true;
            foreach (var containerId in containerIds)
            {
                var container = await client.Containers.InspectContainerAsync(containerId, cancel);
                if (container.State.Running)
                    return false;
                m_logger.LogInformation("Container {ContainerId} has exited with code {Code}", containerId, container.State.ExitCode);
                var logStream = await client.Containers.GetContainerLogsAsync(
                    containerId,
                    true,
                    new ContainerLogsParameters
                    {
                        ShowStdout = true,
                        ShowStderr = true,
                        Tail = "100",
                        Timestamps = true
                    }, cancel);
                var logOutput = await logStream.ReadOutputToEndAsync(cancel);
                if (!string.IsNullOrWhiteSpace(logOutput.stdout))
                    m_logger.LogInformation(logOutput.stdout);
                if (!string.IsNullOrWhiteSpace(logOutput.stderr))
                    m_logger.LogError(logOutput.stderr);
            }

            return true;
        }
        catch (Exception e)
        {
            m_logger.LogWarning(e, "Failed to check if containers have exited");
            return false;
        }
    }

    private async Task StopAndRemovePbAsync(DockerClient client, string containerId, CancellationToken cancel)
    {
        await client.Containers.StopContainerAsync(containerId, new ContainerStopParameters
        {
            WaitBeforeKillSeconds = 2,
        }, cancel);
        await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
        {
            Force = true,
        }, cancel);
        m_logger.LogInformation("Stopped and removed container {ContainerId}", containerId);
    }

    private async Task<string[]> FindPDoriContainers(DockerClient client, CancellationToken cancel)
    {
        var containers = await client.Containers.ListContainersAsync(new ContainersListParameters()
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>()
            {
                {
                    "label",
                    new Dictionary<string, bool>()
                    {
                        { PbDoriLabel, true}
                    }
                }
            }
        }, cancel);
        var containerIds = containers.Select(c => c.ID).ToArray();
        return containerIds;
    }
}