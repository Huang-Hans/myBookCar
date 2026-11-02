using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Extensions;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Credentials.Ports;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Notifications.Ports;
using LMS.Dispatch.Infrastructure.Notifications;
using LMS.Dispatch.Application.Inspections.Ports;
using LMS.Dispatch.Application.Preferences.Ports;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Application.Vehicles.Ports;
using LMS.Dispatch.Infrastructure.Booking;
using LMS.Dispatch.Infrastructure.Platform;
using LMS.Dispatch.Infrastructure.Repositories;
using LMS.Dispatch.Infrastructure.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Infrastructure.Extensions;

public static class DispatchInfrastructureExtensions
{
    // 组合根注册入口：Api 通过它装配持久化与 Platform 读取，端点代码只面向 Application 接口。
    public static IServiceCollection AddDispatchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddScoped<IPropertyReferenceReader, MongoPropertyReferenceReader>();

        services.AddSingleton<ITransactionRunner, MongoTransactionRunner>();
        services.AddScoped<IDriverRepository, MongoDriverRepository>();
        services.AddScoped<IDriverCredentialReader, MongoDriverCredentialReader>();
        services.AddScoped<IDriverOfferRepository, MongoDriverOfferRepository>();
        services.AddScoped<IDriverInspectionRepository, MongoDriverInspectionRepository>();
        services.AddScoped<IVehicleIssueRepository, MongoVehicleIssueRepository>();
        services.AddScoped<IDriverIncidentRepository, MongoDriverIncidentRepository>();
        services.AddScoped<IDriverPreferencesRepository, MongoDriverPreferencesRepository>();
        services.AddScoped<IDriverShiftRepository, MongoDriverShiftRepository>();
        services.AddScoped<IDriverShiftPeriodReader, MongoDriverShiftRepository>();
        services.AddScoped<IShiftBatchRepository, MongoShiftBatchRepository>();
        services.AddScoped<IVehicleRepository, MongoVehicleRepository>();
        services.AddScoped<ICouponRepository, MongoCouponRepository>();
        services.AddScoped<ICouponRedemptionRepository, MongoCouponRedemptionRepository>();
        services.AddScoped<IAuditRecordRepository, MongoAuditRecordRepository>();
        services.AddScoped<IIdempotencyRecordRepository, MongoIdempotencyRecordRepository>();

        // 实时通知：事务内登记待发送事件。事件存储本身由 AddLmsRealtime 注册。
        services.AddScoped<INotificationEventRecorder, MongoNotificationEventRecorder>();

        services.AddOptions<PlatformClientOptions>().Bind(configuration.GetSection(PlatformClientOptions.SectionName));
        services.AddHttpClient<IPlatformDirectory, PlatformDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));

        services.AddOptions<BookingClientOptions>().Bind(configuration.GetSection(BookingClientOptions.SectionName));
        services.AddHttpClient<IBookingAssignments, BookingAssignmentClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BookingClientOptions>>().Value.BaseAddress));

        services.AddHttpClient<IBookingDispatchOrders, BookingDispatchOrdersClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<BookingClientOptions>>().Value.BaseAddress));

        return services;
    }
}
