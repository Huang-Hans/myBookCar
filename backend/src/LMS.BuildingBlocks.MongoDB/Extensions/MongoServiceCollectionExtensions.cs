using LMS.BuildingBlocks.MongoDB.Health;
using LMS.BuildingBlocks.MongoDB.Options;
using LMS.BuildingBlocks.MongoDB.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Extensions;

public static class MongoServiceCollectionExtensions
{
    // 与 LMS.ServiceDefaults 的就绪探针标签一致，/readyz 因此包含 MongoDB 依赖探针。
    private const string ReadinessTag = "ready";

    // 每个进程、每套连接配置只创建一个 MongoClient，以复用连接池。
    // 配置缺失或格式错误会在主机启动时失败，不回退到 localhost。
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<MongoOptions>, MongoOptionsValidator>();
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(CreateClient);
        services.AddSingleton(CreateDatabase);
        services.AddSingleton<IMongoTransactionExecutor, MongoTransactionExecutor>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", tags: [ReadinessTag]);

        return services;
    }

    private static IMongoClient CreateClient(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<MongoOptions>>().Value;
        var settings = MongoClientSettings.FromConnectionString(options.ConnectionString);
        settings.MaxConnectionPoolSize = options.MaxConnectionPoolSize;
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(options.ServerSelectionTimeoutSeconds);
        settings.WriteConcern = WriteConcern.WMajority;

        return new MongoClient(settings);
    }

    private static IMongoDatabase CreateDatabase(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<MongoOptions>>().Value;

        return services.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
    }
}
