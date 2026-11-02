using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Http.Extensions;
using LMS.BuildingBlocks.Realtime.Hubs;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Authorization;
using LMS.Booking.Api.Notifications;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.TripLocations.Ports;

namespace LMS.Booking.Api.Extensions;

public static class BookingApiExtensions
{
    public static IServiceCollection AddBookingApi(this IServiceCollection services)
    {
        services.AddApiControllers();
        services.AddHttpContextAccessor();
        services.AddScoped<ICallerToken, HttpContextCallerToken>();
        services.AddScoped<ICallerAuthorizationResolver, PlatformCallerAuthorizationResolver>();
        services.AddScoped<IPrincipalRecordVerifier, CustomerPrincipalRecordVerifier>();
        AddRealtimeNotifications(services);

        return services;
    }

    // 实时通知的 Booking 侧装配：Hub 授权、发布到客服 / 客户 Hub、在线连接核验。SignalR 本身由 AddLmsRealtime 注册。
    private static void AddRealtimeNotifications(IServiceCollection services)
    {
        services.AddScoped<NotificationCallerTokenScope>();
        services.AddScoped<StaffHubAuthorizer>();
        services.AddScoped<CustomerHubAuthorizer>();
        services.AddScoped<INotificationDeliverer, BookingNotificationDeliverer>();
        services.AddScoped<ITripLocationPublisher, SignalRTripLocationPublisher>();
        services.AddScoped<INotificationConnectionVerifier, BookingNotificationConnectionVerifier>();
    }
}
