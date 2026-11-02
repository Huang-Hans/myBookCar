using LMS.Notification.Application.History.Handlers;
using LMS.Notification.Application.History.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LMS.Notification.Application.Extensions;

public static class NotificationApplicationExtensions
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<NotificationHistoryOptions>().Bind(configuration.GetSection(NotificationHistoryOptions.SectionName));
        services.AddScoped<INotificationHistoryQueryHandler, NotificationHistoryQueryHandler>();

        return services;
    }
}
