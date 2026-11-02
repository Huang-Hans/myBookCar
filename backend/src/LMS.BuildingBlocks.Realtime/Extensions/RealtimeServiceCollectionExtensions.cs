using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Realtime.Delivery;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.BuildingBlocks.Realtime.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LMS.BuildingBlocks.Realtime.Extensions;

public static class RealtimeServiceCollectionExtensions
{
    // 客户端只会调用只读的快照方法，请求体很小；限制接收大小以缩小攻击面。
    private const int MaximumReceiveMessageBytes = 32 * 1024;

    // 注册 SignalR、待发送事件存储、后台发送任务与连接守卫。承载服务另需注册 INotificationDeliverer、
    // INotificationConnectionVerifier（以及按物业扇出时的 INotificationFanOut）并映射自己的 Hub。
    // 消息 JSON：camelCase，null 显式保留（不沿用旧 DTO 省略空值的规则）。
    public static IServiceCollection AddLmsRealtime(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IValidateOptions<NotificationDeliveryOptions>, NotificationDeliveryOptionsValidator>();
        services.AddOptions<NotificationDeliveryOptions>()
            .Bind(configuration.GetSection(NotificationDeliveryOptions.SectionName))
            .ValidateOnStart();

        services.AddSignalR(hub => hub.MaximumReceiveMessageSize = MaximumReceiveMessageBytes)
            .AddJsonProtocol(protocol =>
            {
                protocol.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                protocol.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            });

        services.AddSingleton<NotificationConnectionRegistry>();
        services.AddHostedService<NotificationConnectionGuard>();
        services.AddScoped<MongoNotificationOutbox>();
        services.AddScoped<INotificationOutbox>(provider => provider.GetRequiredService<MongoNotificationOutbox>());
        services.AddScoped<NotificationDispatcher>();
        services.AddHostedService<NotificationDispatchWorker>();

        return services;
    }
}
