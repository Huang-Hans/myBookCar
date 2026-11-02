using LMS.Dispatch.Application.ReportingSources.Handlers;
using LMS.Dispatch.Application.Credentials.Handlers;
using LMS.Dispatch.Application.Drivers.Handlers;
using LMS.Dispatch.Application.Drivers.Options;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Idempotency.Options;
using LMS.Dispatch.Application.Inspections.Handlers;
using LMS.Dispatch.Application.Preferences.Handlers;
using LMS.Dispatch.Application.Reports.Handlers;
using LMS.Dispatch.Application.Reports.Options;
using LMS.Dispatch.Application.Notifications.Services;
using LMS.Dispatch.Application.Offers.Handlers;
using LMS.Dispatch.Application.Offers.Options;
using LMS.Dispatch.Application.Orders.Handlers;
using LMS.Dispatch.Application.Orders.Options;
using LMS.Dispatch.Application.Orders.Services;
using LMS.Dispatch.Application.Offers.Services;
using LMS.Dispatch.Application.Self.Handlers;
using LMS.Dispatch.Application.Self.Services;
using LMS.Dispatch.Application.Shifts.Handlers;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Application.Coupons.Handlers;
using LMS.Dispatch.Application.Vehicles.Handlers;
using LMS.Dispatch.Application.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Extensions;

public static class DispatchApplicationExtensions
{
    public static IServiceCollection AddDispatchApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IValidateOptions<IdempotencyOptions>, IdempotencyOptionsValidator>();
        services.AddOptions<IdempotencyOptions>()
            .Bind(configuration.GetSection(IdempotencyOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<DispatchBusinessOptions>().Bind(configuration.GetSection(DispatchBusinessOptions.SectionName));
        services.AddOptions<OfferOptions>().Bind(configuration.GetSection(OfferOptions.SectionName));
        services.AddOptions<DispatchQueueOptions>().Bind(configuration.GetSection(DispatchQueueOptions.SectionName));
        services.AddOptions<OperationsContactsOptions>().Bind(configuration.GetSection(OperationsContactsOptions.SectionName));

        services.AddSingleton<BusinessCalendar>();
        services.AddScoped<CreateWorkflow>();
        services.AddScoped<PatchWorkflow>();
        services.AddScoped<IDriverQueryHandler, DriverQueryHandler>();
        services.AddScoped<ICreateDriverHandler, CreateDriverHandler>();
        services.AddScoped<IUpdateDriverHandler, UpdateDriverHandler>();
        services.AddScoped<IRemoveDriverHandler, RemoveDriverHandler>();
        services.AddScoped<DriverVehicleBinding>();
        services.AddScoped<ICheckDriverStatusesHandler, CheckDriverStatusesHandler>();
        services.AddScoped<IReleaseDriverHandler, ReleaseDriverHandler>();
        services.AddScoped<IVerifyDriverCredentialsHandler, VerifyDriverCredentialsHandler>();
        services.AddScoped<ICheckDriverLoginEligibilityHandler, CheckDriverLoginEligibilityHandler>();

        AddSelfServiceHandlers(services);
        AddDriverRecordHandlers(services);
        AddDispatchQueueHandlers(services);
        AddShiftHandlers(services);
        AddVehicleHandlers(services);
        AddCouponHandlers(services);

        return services;
    }

    // 司机本人（工作状态、邀请）：本人标识来自令牌绑定，处理器只面向本人范围。
    private static void AddSelfServiceHandlers(IServiceCollection services)
    {
        services.AddScoped<DriverAvailabilityReconciler>();
        services.AddScoped<IDriverSelfQueryHandler, DriverSelfQueryHandler>();
        services.AddScoped<IChangeDriverAvailabilityHandler, ChangeDriverAvailabilityHandler>();

        services.AddScoped<OfferNotificationRecorder>();
        services.AddScoped<DriverNotificationAccess>();
        services.AddScoped<OfferClosingService>();
        services.AddScoped<OfferAcceptanceCommitter>();
        services.AddScoped<OfferAcceptanceService>();
        services.AddScoped<IDriverOfferQueryHandler, DriverOfferQueryHandler>();
        services.AddScoped<ICreateDriverOfferHandler, CreateDriverOfferHandler>();
        services.AddScoped<IRespondToOfferHandler, RespondToOfferHandler>();
    }

    // 客服派单：队列读取、候选司机、邀请历史，以及邀请创建的校验链与过期处理。
    private static void AddDispatchQueueHandlers(IServiceCollection services)
    {
        services.AddScoped<OfferExpirationService>();
        services.AddScoped<IOfferExpirationSweeper>(provider => provider.GetRequiredService<OfferExpirationService>());
        services.AddScoped<OfferCreationGuard>();
        services.AddScoped<OfferReplayProbe>();
        services.AddScoped<DispatchQueuePlanner>();
        services.AddScoped<DispatchOrderComposer>();
        services.AddScoped<DispatchOfferReconciler>();
        services.AddScoped<IDispatchOrderQueryHandler, DispatchOrderQueryHandler>();
        services.AddScoped<IDispatchCandidateQueryHandler, DispatchCandidateQueryHandler>();
        services.AddScoped<IDispatchOfferHistoryHandler, DispatchOfferHistoryHandler>();
    }

    // 司机端第二阶段：检查、车辆问题、事故报告、运营联系与偏好。本人标识来自令牌，处理器只面向本人范围。
    private static void AddDriverRecordHandlers(IServiceCollection services)
    {
        services.AddScoped<ISubmitInspectionHandler, SubmitInspectionHandler>();
        services.AddScoped<IDriverInspectionQueryHandler, DriverInspectionQueryHandler>();
        services.AddScoped<ICreateVehicleIssueHandler, CreateVehicleIssueHandler>();
        services.AddScoped<IVehicleIssueQueryHandler, VehicleIssueQueryHandler>();
        services.AddScoped<ICreateIncidentHandler, CreateIncidentHandler>();
        services.AddScoped<IIncidentQueryHandler, IncidentQueryHandler>();
        services.AddScoped<IOperationsContactsQueryHandler, OperationsContactsQueryHandler>();
        services.AddScoped<IDriverPreferencesQueryHandler, DriverPreferencesQueryHandler>();
        services.AddScoped<IUpdateDriverPreferencesHandler, UpdateDriverPreferencesHandler>();
    }

    // 排班：先把司机排进某一天的早 / 午 / 夜班，再为班次派车。
    private static void AddShiftHandlers(IServiceCollection services)
    {
        services.AddScoped<ShiftCardComposer>();
        services.AddScoped<PickupShiftQuery>();
        services.AddScoped<ShiftWithdrawalGuard>();
        services.AddScoped<IWithdrawShiftsHandler, WithdrawShiftsHandler>();
        services.AddScoped<ShiftBatchPlanner>();
        services.AddScoped<ShiftBatchPropertyCheck>();
        services.AddScoped<ShiftBatchWriter>();
        services.AddScoped<ShiftBatchReplayProbe>();
        services.AddScoped<IShiftBatchPreviewHandler, ShiftBatchPreviewHandler>();
        services.AddScoped<ICreateShiftBatchHandler, CreateShiftBatchHandler>();
        services.AddScoped<IShiftBoardQueryHandler, ShiftBoardQueryHandler>();
        services.AddScoped<IReportingShiftsQueryHandler, ReportingShiftsQueryHandler>();
        services.AddScoped<ICreateShiftHandler, CreateShiftHandler>();
        services.AddScoped<IUpdateShiftHandler, UpdateShiftHandler>();
        services.AddScoped<IAssignShiftVehicleHandler, AssignShiftVehicleHandler>();
        services.AddScoped<IDriverShiftListQueryHandler, DriverShiftListQueryHandler>();
    }

    // 车辆主档：列表、顶部四态统计，以及新增。
    private static void AddVehicleHandlers(IServiceCollection services)
    {
        services.AddScoped<IVehicleQueryHandler, VehicleQueryHandler>();
        services.AddScoped<ICreateVehicleHandler, CreateVehicleHandler>();
    }

    // 優惠券：列表、頂部統計、詳情與新增。每張券綁定一名會員。
    private static void AddCouponHandlers(IServiceCollection services)
    {
        services.AddScoped<ICouponQueryHandler, CouponQueryHandler>();
        services.AddScoped<ICreateCouponHandler, CreateCouponHandler>();
        services.AddScoped<IUpdateCouponHandler, UpdateCouponHandler>();
        services.AddScoped<IRemoveCouponHandler, RemoveCouponHandler>();
        services.AddScoped<ICouponRedemptionHandler, CouponRedemptionHandler>();
    }
}
