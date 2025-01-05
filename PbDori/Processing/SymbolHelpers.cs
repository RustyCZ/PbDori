using CryptoExchange.Net.CommonObjects;

namespace PbDori.Processing;

public static class SymbolHelpers
{
    public static string NormalizeCoin(string coin)
    {
        var normalizedCoin = coin
            .Replace("10000000000", "")
            .Replace("1000000000", "")
            .Replace("100000000", "")
            .Replace("10000000", "")
            .Replace("1000000", "")
            .Replace("100000", "")
            .Replace("10000", "")
            .Replace("1000", "")
            .Replace("USDT", "");
        return normalizedCoin;
    }

    public static bool IsStableCoinPair(string symbol)
    {
        if (string.Equals(symbol, "USDCUSDT", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public static bool IsTradedQuotedAsset(string asset)
    {
        if (string.Equals(asset, "USDT", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
