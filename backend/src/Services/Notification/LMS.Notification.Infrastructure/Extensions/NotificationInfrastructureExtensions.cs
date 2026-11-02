using LMS.BuildingBlocks.MongoDB.Extensions;
using LMS.Notification.Application.Directory;
using LMS.Notification.Application.History.Ports;
using LMS.Notification.Infrastructure.Platform;
using LMS.Notification.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Notification.Infrastructure.Extensions;

public static class NotificationInfrastructureExtensions
{
    // 组合根注册入口：Api 通过它装配持久化与 Platform 读取，端点代码只面向 Application 接口。
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);

        services.AddScoped<INotificationHistoryReader, MongoNotificationHistoryReader>();

        services.AddOptions<PlatformClientOptions>().Bind(configuration.GetSection(PlatformClientOptions.SectionName));
        services.AddHttpClient<IPlatformDirectory, PlatformDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));

        return services;
    }
}
