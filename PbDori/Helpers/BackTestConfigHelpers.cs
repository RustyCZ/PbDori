using Hjson;
using PbDori.Model;
using System.Text.Json;

namespace PbDori.Helpers;

public static class BackTestConfigHelpers
{
    public static string GenerateBackTestConfig(string[] symbols, DateTime start, DateTime end)
    {
        BackTestConfig config = new BackTestConfig
        {
            Symbols = symbols,
            StartDate = start.ToString("yyyy-MM-dd"),
            EndDate = end.ToString("yyyy-MM-dd"),
        };
        var serializedConfig = config.SerializeConfig();
        return serializedConfig;
    }

    public static string SerializeConfig(this BackTestConfig config)
    {
        string jsonString = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        JsonValue v = JsonValue.Parse(jsonString);
        var serializedConfig = v.ToString(Stringify.Hjson);
        return serializedConfig;
    }
}
