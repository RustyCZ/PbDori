namespace PbDori.Configuration;

public class PbFileSystem
{
    public string HostDataDir { get; set; } = "Data";
    public string AppDataDir { get; set; } = "Data";
    public string MountApiKeysPath { get; set; } = "passivbot/api-keys.json";
    public string MountConfigsPath { get; set; } = "passivbot/configs";
    public string MountBackTestsPath { get; set; } = "passivbot/backtests";
    public string MountHistoricalDataPath { get; set; } = "passivbot/historical_data";
    public string TmpPath { get; set; } = "passivbot/tmp";
    public string BackTestConfigPathHost { get; set; } = "passivbot/configs/backtest/default.hjson";
    public string BackTestResultsPath { get; set; } = "passivbot/backtests/bybit";
}
