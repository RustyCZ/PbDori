
using AspNetCore.Authentication.Basic;
using Microsoft.AspNetCore.Authorization;
using PbDori.Authentication;
using PbDori.CoinMarketCap;
using PbDori.Helpers;
using PbDori.PbLifeCycle;
using PbDori.Processing;
using PbDori.Repositories;
using PbDori.Services;

namespace PbDori;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddEnvironmentVariables("PBDORI_");
        var configuration = builder.Configuration.GetSection("PbDori").Get<Configuration.PbDori>();
        if (configuration == null)
            throw new InvalidOperationException("Missing configuration.");

        bool useBasicAuth = !string.IsNullOrWhiteSpace(configuration.ApiBasicAuth.Username) &&
                                 !string.IsNullOrWhiteSpace(configuration.ApiBasicAuth.Password);
        if (useBasicAuth)
        {
            builder.Services.AddOptions<BasicUserValidationServiceOptions>().Configure(o =>
            {
                o.Password = configuration.ApiBasicAuth.Password;
                o.Username = configuration.ApiBasicAuth.Username;
            });
            builder.Services.AddAuthentication(BasicDefaults.AuthenticationScheme)
                .AddBasic<BasicUserValidationService>(options => { options.Realm = "PbDori"; });
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });
        }

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddOptions<PbLifeCycleControllerOptions>().Configure(x =>
        {
            x.MountApiKeysPath = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.HostDataDir, configuration.PbFileSystem.MountApiKeysPath));
            x.MountConfigsPath = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.HostDataDir, configuration.PbFileSystem.MountConfigsPath));
            x.MountBackTestsPath = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.HostDataDir, configuration.PbFileSystem.MountBackTestsPath));
            x.MountHistoricalDataPath = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.HostDataDir, configuration.PbFileSystem.MountHistoricalDataPath));
            x.MountTmpPath = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.HostDataDir, configuration.PbFileSystem.TmpPath));
            x.BackTestConfigPathHost = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.AppDataDir, configuration.PbFileSystem.BackTestConfigPathHost));
        });
        builder.Services.AddOptions<BackTestResultProcessorOptions>().Configure(x =>
        {
            x.BasePath = Path.GetFullPath(Path.Combine(configuration.PbFileSystem.AppDataDir, configuration.PbFileSystem.BackTestResultsPath));
        });
        builder.Services.AddSingleton<IPbLifeCycleController, PbLifeCycleController>();
        builder.Services.AddSingleton<IBackTestResultProcessor, BackTestResultProcessor>();
        builder.Services.AddSingleton<ISymbolDataProvider, BybitSymbolDataProvider>();
        builder.Services.AddBybit();
        builder.Services.AddSingleton<IStrategyResultRepository, StrategyResultRepository>();
        builder.Services.AddOptions<StrategyResultRepositoryOptions>().Configure(x =>
        {
            x.ResultsFile = PathHelpers.GetFullPathWindowsCompatible(Path.Combine(configuration.PbFileSystem.AppDataDir ,configuration.BackTestResultsStorage.ResultsFile));
        });
        builder.Services.AddHostedService<BackTestService>();
        builder.Services.AddOptions<BackTestServiceOptions>().Configure(x =>
        {
            x.Interval = TimeSpan.FromHours(2);
            x.MaxExecutionDuration = TimeSpan.FromHours(3);
            x.Strategies = configuration.Strategies;
        });
        builder.Services.AddSingleton<ICoinMarketCapClient, CoinMarketCapClient>();
        builder.Services.AddOptions<CoinMarketCapClientOptions>().Configure(x =>
        {
            x.ApiKey = configuration.CoinMarketCap.ApiKey;
            x.CacheDuration = configuration.CoinMarketCap.CacheDuration;
            x.CoinLimit = configuration.CoinMarketCap.CoinLimit;
        });
        builder.Services.AddSingleton<IBlacklistedSymbolsProvider, ConfiguredBlacklistedSymbolsProvider>();
        builder.Services.AddOptions<ConfiguredBlacklistedSymbolsProviderOptions>().Configure(x =>
        {
            x.BlacklistedSymbols = configuration.Blacklist.Symbols;
        });
        builder.Services.AddSingleton<IMarketTrendProcessor, MarketTrendProcessor>();
        if (configuration.MarketTrend.Enable)
        {
            builder.Services.AddHostedService<MarketTrendService>();
            builder.Services.AddOptions<MarketTrendServiceOptions>().Configure(x =>
            {
                x.Interval = TimeSpan.FromHours(1);
            });
        }
        builder.Services.AddSingleton<IMarketTrendResultRepository, InMemoryIMarketTrendResultRepository>();
        builder.Services.AddLogging(options =>
        {
            options.AddSimpleConsole(o =>
            {
                o.UseUtcTimestamp = true;
                o.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            });
        });

        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
