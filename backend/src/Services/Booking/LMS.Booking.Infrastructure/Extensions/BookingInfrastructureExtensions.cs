using LMS.BuildingBlocks.Core.References;
using LMS.BuildingBlocks.MongoDB.Extensions;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Infrastructure.Notifications;
using LMS.Booking.Infrastructure.ServiceIdentity;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Approvals.Ports;
using LMS.Booking.Application.Auditing.Ports;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.ConsoleNotifications.Ports;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Application.Coupons;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Credentials.Ports;
using LMS.Booking.Application.DispatchOrders.Ports;
using LMS.Booking.Application.DriverTrips.Ports;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Idempotency.Ports;
using LMS.Booking.Application.Overrides.Ports;
using LMS.Booking.Application.Qualifications.Ports;
using LMS.Booking.Application.Quotas.Ports;
using LMS.Booking.Application.Reviews.Ports;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Staff.Ports;
using LMS.Booking.Application.ReportingSources.Ports;
using LMS.Booking.Application.TripLocations.Ports;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Infrastructure.Remote;
using LMS.Booking.Infrastructure.Repositories;
using LMS.Booking.Infrastructure.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Infrastructure.Extensions;

public static class BookingInfrastructureExtensions
{
    // 组合根注册入口：Api 通过它装配持久化与 Platform / Dispatch 读取，端点代码只面向 Application 接口。
    public static IServiceCollection AddBookingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMongoDb(configuration);
        services.AddScoped<IPropertyReferenceReader, MongoPropertyReferenceReader>();

        services.AddSingleton<ITransactionRunner, MongoTransactionRunner>();
        services.AddScoped<IPropertyQuotaRepository, MongoPropertyQuotaRepository>();
        services.AddScoped<IQuotaOverrideRepository, MongoQuotaOverrideRepository>();
        services.AddScoped<IQualificationRepository, MongoQualificationRepository>();
        services.AddScoped<IApprovalRepository, MongoApprovalRepository>();
        services.AddScoped<IQualificationAuditReader, MongoQualificationAuditReader>();
        services.AddScoped<IAuditRecordRepository, MongoAuditRecordRepository>();
        services.AddScoped<IIdempotencyRecordRepository, MongoIdempotencyRecordRepository>();

        // 客服控制台（一期）：行程、宾客读模型、站内通知与员工偏好。
        services.AddScoped<ITripRepository, MongoTripRepository>();
        services.AddScoped<ICustomerCredentialReader, MongoCustomerCredentialReader>();
        services.AddScoped<IDriverTripRepository, MongoTripRepository>();
        services.AddScoped<IGuestTripRepository, MongoTripRepository>();
        services.AddScoped<IDispatchOrderRepository, MongoTripRepository>();
        services.AddScoped<IReportingTripRepository, MongoTripRepository>();
        services.AddScoped<ITripSummaryRepository, MongoTripRepository>();
        services.AddScoped<IGuestDirectory, MongoGuestDirectory>();
        services.AddScoped<IGuestProfileRepository, MongoGuestProfileRepository>();
        services.AddScoped<ILoyaltyGateway, ReferenceLoyaltyGateway>();
        services.AddScoped<IConsoleNotificationRepository, MongoConsoleNotificationRepository>();
        services.AddScoped<IStaffPreferencesRepository, MongoStaffPreferencesRepository>();
        services.AddScoped<ICustomerAccountProfileRepository, MongoCustomerAccountProfileRepository>();
        services.AddScoped<ICustomerPreferencesRepository, MongoCustomerPreferencesRepository>();
        services.AddScoped<ICustomerLocationFavoritesRepository, MongoCustomerLocationFavoritesRepository>();
        services.AddScoped<ITripReviewRepository, MongoTripReviewRepository>();
        services.AddScoped<ITripLiveLocationRepository, MongoTripLiveLocationRepository>();
        services.AddScoped<ITripLocationTrackRepository, MongoTripLocationTrackRepository>();

        // 实时通知：事务内登记待发送事件、客服下单通知快照与受限服务身份。事件存储本身由 AddLmsRealtime 注册。
        services.AddScoped<MongoNotificationEventRecorder>();
        services.AddScoped<INotificationEventRecorder>(provider => provider.GetRequiredService<MongoNotificationEventRecorder>());
        services.AddScoped<IStaffFanOutStore>(provider => provider.GetRequiredService<MongoNotificationEventRecorder>());
        services.AddScoped<IStaffNotificationSnapshotRepository, MongoStaffNotificationSnapshotRepository>();
        services.AddOptions<ServiceSigningOptions>().Bind(configuration.GetSection(ServiceSigningOptions.SectionName));
        services.AddSingleton<IServiceTokenIssuer, ServiceTokenSigner>();

        services.AddOptions<PlatformClientOptions>().Bind(configuration.GetSection(PlatformClientOptions.SectionName));
        services.AddOptions<DispatchClientOptions>().Bind(configuration.GetSection(DispatchClientOptions.SectionName));
        services.AddHttpClient<IPlatformDirectory, PlatformDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));
        services.AddHttpClient<IDriverDirectory, DriverDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<DispatchClientOptions>>().Value.BaseAddress));
        services.AddHttpClient<IDriverContactDirectory, DriverContactDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<DispatchClientOptions>>().Value.BaseAddress));
        // 行程结束后释放司机：Dispatch 的内部受限接口，使用 Booking 自己的服务身份令牌。
        services.AddHttpClient<IDispatchDriverReleases, DispatchDriverReleaseClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<DispatchClientOptions>>().Value.BaseAddress));
        services.AddHttpClient<ICouponRedemptions, CouponRedemptionClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<DispatchClientOptions>>().Value.BaseAddress));
        // Platform 公共只读目录（地点/取消原因/物业政策），复用 Platform 服务发现地址与转发令牌机制。
        services.AddHttpClient<IPlatformCatalog, PlatformCatalogClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));
        // 员工通知收件人目录：Platform 的内部受限接口，使用 Booking 自己的服务身份令牌，不转发终端用户令牌。
        services.AddHttpClient<IStaffNotificationDirectory, PlatformStaffNotificationDirectory>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));
        // 客户自助预订的 Platform 目录（客户专用的服务间端点），同样转发客户本地令牌。
        services.AddHttpClient<IPlatformSelfCatalog, PlatformSelfCatalogClient>((provider, client) =>
            client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PlatformClientOptions>>().Value.BaseAddress));

        return services;
    }
}
