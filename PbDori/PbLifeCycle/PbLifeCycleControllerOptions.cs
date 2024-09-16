namespace PbDori.PbLifeCycle;

public class PbLifeCycleControllerOptions
{
    public string DockerHost { get; set; } = Environment.OSVersion.Platform == PlatformID.Win32NT
        ? "npipe://./pipe/docker_engine"
        : "unix:///var/run/docker.sock";

    public string Image { get; set; } = "passivbot:latest";

    public string ConfigsPath { get; set; } = "/passivbot/configs";

    public string ApiKeysPath { get; set; } = "/passivbot/api-keys.json";

    public string BackTestsPath { get; set; } = "/passivbot/backtests";

    public string HistoricalDataPath { get; set; } = "/passivbot/historical_data";

    public string TmpPath { get; set; } = "/passivbot/tmp";

    public string? MountConfigsPath { get; set; }

    public string? MountApiKeysPath { get; set; }

    public string? MountBackTestsPath { get; set; }

    public string? MountHistoricalDataPath { get; set; }

    public string? MountTmpPath { get; set; }

    public string? BackTestConfigPathHost { get; set; }
}