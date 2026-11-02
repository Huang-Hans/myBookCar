using LMS.BuildingBlocks.Http.Extensions;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Notification.Api.Authorization;
using LMS.Notification.Application.Abstractions;

namespace LMS.Notification.Api.Extensions;

public static class NotificationApiExtensions
{
    public static IServiceCollection AddNotificationApi(this IServiceCollection services)
    {
        services.AddApiControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICallerToken, HttpContextCallerToken>();
        services.AddScoped<ICallerAuthorizationResolver, PlatformCallerAuthorizationResolver>();

        return services;
    }
}
