using LMS.Booking.Application.Approvals.Handlers;
using LMS.Booking.Application.Approvals.Options;
using LMS.Booking.Application.Approvals.Services;
using LMS.Booking.Application.Coupons;
using LMS.Booking.Application.Console.Options;
using LMS.Booking.Application.ConsoleNotifications.Handlers;
using LMS.Booking.Application.ConsoleNotifications.Services;
using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.Booking.Application.Guests.Handlers;
using LMS.Booking.Application.Notifications.Handlers;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.Idempotency.Options;
using LMS.Booking.Application.Overrides.Handlers;
using LMS.Booking.Application.Qualifications.Handlers;
using LMS.Booking.Application.Qualifications.Services;
using LMS.Booking.Application.Credentials.Handlers;
using LMS.Booking.Application.Customers.Handlers;
using LMS.Booking.Application.Customers.Options;
using LMS.Booking.Application.DispatchOrders.Handlers;
using LMS.Booking.Application.Previews.Handlers;
using LMS.Booking.Application.DriverTrips.Handlers;
using LMS.Booking.Application.DriverTrips.Services;
using LMS.Booking.Application.Quotas.Handlers;
using LMS.Booking.Application.Reviews.Handlers;
using LMS.Booking.Application.SelfService.Handlers;
using LMS.Booking.Application.SelfService.Options;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.Staff.Handlers;
using LMS.Booking.Application.ReportingSources.Handlers;
using LMS.Booking.Application.TripLocations.Handlers;
using LMS.Booking.Application.TripLocations.Services;
using LMS.Booking.Application.Trips.Handlers;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Application.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Extensions;

public static class BookingApplicationExtensions
{
    public static IServiceCollection AddBookingApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<ApprovalOptions>().Bind(configuration.GetSection(ApprovalOptions.SectionName));
        services.AddSingleton<IValidateOptions<SelfServiceOptions>, SelfServiceOptionsValidator>();
        services.AddOptions<SelfServiceOptions>().Bind(configuration.GetSection(SelfServiceOptions.SectionName)).ValidateOnStart();
        services.AddOptions<FavoritesOptions>().Bind(configuration.GetSection(FavoritesOptions.SectionName));
        services.AddSingleton<IValidateOptions<ConsoleOptions>, ConsoleOptionsValidator>();
        services.AddOptions<ConsoleOptions>()
            .Bind(configuration.GetSection(ConsoleOptions.SectionName))
            .ValidateOnStart();

        services.AddScoped<CreateWorkflow>();
        services.AddScoped<PatchWorkflow>();
        services.AddScoped<DecisionWorkflow>();
        services.AddScoped<UpsertWorkflow>();
        AddQuotaAndOverrideHandlers(services);
        AddQualificationHandlers(services);
        AddApprovalHandlers(services);
        AddConsoleHandlers(services);
        AddCredentialHandlers(services);
        AddDriverHandlers(services);
        AddSelfServiceHandlers(services);
        AddCustomerRecordHandlers(services);
        AddRealtimeNotificationServices(services);

        return services;
    }

    private static void AddQuotaAndOverrideHandlers(IServiceCollection services)
    {
        services.AddScoped<IPropertyQuotaQueryHandler, PropertyQuotaQueryHandler>();
        services.AddScoped<IUpdatePropertyQuotaHandler, UpdatePropertyQuotaHandler>();
        services.AddScoped<IQuotaOverrideQueryHandler, QuotaOverrideQueryHandler>();
        services.AddScoped<ICreateQuotaOverrideHandler, CreateQuotaOverrideHandler>();
        services.AddScoped<IDecideQuotaOverrideHandler, DecideQuotaOverrideHandler>();
    }

    private static void AddQualificationHandlers(IServiceCollection services)
    {
        services.AddScoped<QualificationExceptionExecutor>();
        services.AddScoped<IQualificationQueryHandler, QualificationQueryHandler>();
        services.AddScoped<IEscalateQualificationHandler, EscalateQualificationHandler>();
        services.AddScoped<IApproveQualificationExceptionHandler, ApproveQualificationExceptionHandler>();
        services.AddScoped<IRejectQualificationExceptionHandler, RejectQualificationExceptionHandler>();
        services.AddScoped<ICreateQualificationRequestHandler, CreateQualificationRequestHandler>();
    }

    private static void AddApprovalHandlers(IServiceCollection services)
    {
        services.AddScoped<ApprovalReviewChecks>();
        services.AddScoped<IApprovalQueryHandler, ApprovalQueryHandler>();
        services.AddScoped<IUpdateApprovalDocumentHandler, UpdateApprovalDocumentHandler>();
        services.AddScoped<IRegisterApprovalUploadHandler, RegisterApprovalUploadHandler>();
        services.AddScoped<IDecideApprovalHandler, DecideApprovalHandler>();
        services.AddScoped<IBulkDecideApprovalsHandler, BulkDecideApprovalsHandler>();
        services.AddScoped<IBlacklistHitQueryHandler, BlacklistHitQueryHandler>();
    }

    // 客服控制台（一期）：行程、宾客、站内通知与员工偏好。
    private static void AddConsoleHandlers(IServiceCollection services)
    {
        services.AddScoped<VehicleCapacityResolver>();
        services.AddScoped<PropertyQuotaGate>();
        services.AddScoped<TripEligibilityService>();
        services.AddScoped<ConsoleBookingProductRules>();
        services.AddScoped<IConsoleBookingOptionsHandler, ConsoleBookingOptionsHandler>();
        services.AddScoped<QualificationRecorder>();
        services.AddScoped<ConsoleNotificationPublisher>();

        services.AddScoped<CouponLedger>();
        services.AddScoped<ICreateTripHandler, CreateTripHandler>();
        services.AddScoped<IUpdateTripHandler, UpdateTripHandler>();
        services.AddScoped<ICancelTripHandler, CancelTripHandler>();
        services.AddScoped<IAdvanceTripStatusHandler, AdvanceTripStatusHandler>();
        services.AddScoped<ITripQueryHandler, TripQueryHandler>();

        services.AddScoped<IGuestQueryHandler, GuestQueryHandler>();
        services.AddScoped<IUpdateGuestHandler, UpdateGuestHandler>();

        services.AddScoped<IConsoleNotificationQueryHandler, ConsoleNotificationQueryHandler>();
        services.AddScoped<IMarkConsoleNotificationReadHandler, MarkConsoleNotificationReadHandler>();
        services.AddScoped<IMarkAllConsoleNotificationsReadHandler, MarkAllConsoleNotificationsReadHandler>();

        services.AddScoped<IStaffPreferencesQueryHandler, StaffPreferencesHandler>();
        services.AddScoped<IStaffConsoleSettingsUpdateHandler, StaffPreferencesHandler>();
        services.AddScoped<IStaffNotificationPreferencesUpdateHandler, StaffPreferencesHandler>();
        services.AddScoped<IStaffLocationFavoritesUpdateHandler, StaffPreferencesHandler>();
    }

    // 司机端（P1）：指派、履约与本人行程查询。本人标识来自本地令牌，处理器只面向本人范围。
    // 客户本地登录凭据核对（供 Platform 的客户登录与刷新调用）。
    private static void AddCredentialHandlers(IServiceCollection services)
    {
        services.AddScoped<IVerifyCustomerCredentialsHandler, VerifyCustomerCredentialsHandler>();
        services.AddScoped<ICheckCustomerLoginEligibilityHandler, CheckCustomerLoginEligibilityHandler>();
    }

    private static void AddDriverHandlers(IServiceCollection services)
    {
        services.AddScoped<TripEventNotifier>();
        services.AddScoped<TripDriverReleaseNotifier>();
        services.AddScoped<TripLocationEndNotifier>();
        services.AddScoped<IRecordTripLocationsHandler, RecordTripLocationsHandler>();
        services.AddScoped<ITripLocationWatchHandler, TripLocationWatchHandler>();
        services.AddScoped<ITripTrackQueryHandler, TripTrackQueryHandler>();
        services.AddScoped<IReportingTripFactsQueryHandler, ReportingTripFactsQueryHandler>();
        services.AddScoped<IAssignDriverHandler, AssignDriverHandler>();
        services.AddScoped<IRecordTripProgressHandler, RecordTripProgressHandler>();
        services.AddScoped<IDriverTripQueryHandler, DriverTripQueryHandler>();
        services.AddScoped<IDispatchOrderQueryHandler, DispatchOrderQueryHandler>();
        services.AddScoped<IBookingPreviewHandler, BookingPreviewHandler>();
    }

    // 客户端（P1）：自助预订与本人订单。客户身份来自令牌绑定，规则在提交时重新计算。
    private static void AddSelfServiceHandlers(IServiceCollection services)
    {
        services.AddScoped<GuestBookingGate>();
        services.AddScoped<SelfServiceRouteChecker>();
        services.AddScoped<SelfServiceVehicleCatalog>();
        services.AddScoped<SelfServiceEligibilityService>();
        services.AddScoped<SelfCancellationRules>();

        services.AddScoped<ICustomerProfileHandler, CustomerProfileHandler>();
        services.AddScoped<ICustomerOfferingsHandler, CustomerOfferingsHandler>();
        services.AddScoped<ICustomerLocationsHandler, CustomerLocationsHandler>();
        services.AddScoped<ICustomerRideOptionsHandler, CustomerRideOptionsHandler>();
        services.AddScoped<ICreateSelfServiceTripHandler, CreateSelfServiceTripHandler>();
        services.AddScoped<ICustomerTripQueryHandler, CustomerTripQueryHandler>();
        services.AddScoped<ICustomerCancellationHandler, CustomerCancellationHandler>();
    }

    // 实时通知：事务内登记事件、按物业扇出给授权客服、发布前复核员工授权、客服通知快照。
    private static void AddRealtimeNotificationServices(IServiceCollection services)
    {
        services.AddScoped<TripNotificationRecorder>();
        services.AddScoped<StaffDeliveryGate>();
        services.AddScoped<INotificationFanOut, StaffNotificationFanOut>();
        services.AddScoped<IStaffNotificationSnapshotHandler, StaffNotificationSnapshotHandler>();
    }

    // 客户端第二阶段：本地账户资料、偏好、两组地点收藏与行程评价。客户身份来自令牌，处理器只面向本人范围。
    private static void AddCustomerRecordHandlers(IServiceCollection services)
    {
        services.AddScoped<ICustomerAccountHandler, CustomerAccountHandler>();
        services.AddScoped<ICustomerPreferencesHandler, CustomerPreferencesHandler>();
        services.AddScoped<ILocationFavoritesHandler, LocationFavoritesHandler>();
        services.AddScoped<IReviewableTripsQueryHandler, ReviewableTripsQueryHandler>();
        services.AddScoped<ITripReviewHandler, TripReviewHandler>();
    }
}
