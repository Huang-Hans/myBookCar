using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Application.Notifications.Services;
using LMS.Booking.Application.SelfService.Commands;
using LMS.Booking.Application.SelfService.Models;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Application.Trips.Services;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Handlers;

// 客户自助建单：服务端从令牌绑定得到客户，所有资格 / 时间 / 路线 / 配额规则在提交时重新计算，再经 CreateWorkflow
// （幂等键 + 行程与审计同事务）落库。订单事实只在服务端：下单成功后可按订单 ID 查询到同一份持久化结果。
// 不写员工偏好（最近地点）、不登记客服资格档案；候补或跨境待审批的订单受理为 PENDING，不显示为已确认。
internal sealed class CreateSelfServiceTripHandler(
    CreateWorkflow workflow,
    SelfServiceEligibilityService eligibility,
    ITripRepository trips,
    ICustomerPreferencesRepository preferences,
    TripNotificationRecorder notifications,
    TimeProvider timeProvider) : ICreateSelfServiceTripHandler
{
    private const string Operation = "trip.self-create";
    private const string ResourceType = "trip";

    // 自助订单的经办人角色：经办人是客户本人，不是员工。
    private const string CustomerRole = "CUSTOMER";

    public async Task<OperationResult<Trip>> HandleAsync(CreateSelfServiceTripCommand command, CancellationToken cancellationToken)
    {
        var verdict = await eligibility.EvaluateAsync(command.Booking, cancellationToken);
        if (!verdict.IsSuccess)
        {
            return OperationResult<Trip>.FailureOf(verdict);
        }

        var language = await ResolveLanguageAsync(command, cancellationToken);

        return await workflow.ExecuteAsync(BuildSpecification(command, verdict.Value!, language), cancellationToken);
    }

    // 确认语言：优先显式请求，其次本人偏好，再其次约定默认；已有订单的确认语言不会随偏好变化而追溯修改。
    private async Task<string> ResolveLanguageAsync(CreateSelfServiceTripCommand command, CancellationToken cancellationToken)
    {
        if (command.ConfirmationLanguage is not null)
        {
            return command.ConfirmationLanguage;
        }

        var saved = await preferences.FindAsync(command.Booking.GuestId, cancellationToken);

        return WireEnum.ToWire(saved?.Language ?? CustomerPreferences.DefaultLanguage);
    }

    private CreateSpecification<Trip> BuildSpecification(CreateSelfServiceTripCommand command, SelfBookingApproval approval, string language) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            async (context, token) => BuildTrip(command, approval, language, await trips.NextReferenceAsync(MacauClock.DateOf(timeProvider.GetUtcNow()), context, token)),
            trip => trip.TripId,
            (trip, context, token) => InsertAndRecordAsync(trip, command.Context.TraceId, context, token),
            (id, token) => trips.FindByIdAsync(id, token),
            (trip, occurredAt) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, trip.TripId, occurredAt, trip.PropertyId));

    // 订单与“新订单”实时通知事件在同一个事务内提交；幂等重放不会走到这里，因此不会重复登记。
    private async Task InsertAndRecordAsync(Trip trip, string traceId, ITransactionContext context, CancellationToken cancellationToken)
    {
        await trips.InsertAsync(trip, context, cancellationToken);
        await notifications.RecordBookingCreatedAsync(trip, traceId, context, cancellationToken);
    }

    private Trip BuildTrip(CreateSelfServiceTripCommand command, SelfBookingApproval approval, string language, string reference)
    {
        var guest = approval.Guest;
        var booking = command.Booking;

        return Trip.Create(
            reference,
            guest.PropertyId!,
            new TripGuest(guest.GuestId, guest.Name, guest.TierCode, guest.RoomNumber, guest.MemberId, guest.MaskedPhone),
            approval.Route,
            approval.PickupAt,
            booking.PickupMode,
            ServiceTypeOf(approval),
            booking.VehicleClass,
            booking.Passengers,
            booking.Luggage,
            command.SpecialRequests,
            approval.Capacity.PointsRequired,
            new BookedBy(guest.GuestId, CustomerRole, guest.Name),
            language,
            timeProvider.GetUtcNow(),
            TripChannel.SelfService,
            ProductOf(approval));
    }

    // 产品到履约行程类型的显式映射来自部署配置；配置值不合法属于部署错误，立即暴露而不是静默回退。
    private static ServiceType ServiceTypeOf(SelfBookingApproval approval) =>
        WireEnum.TryParse<ServiceType>(approval.Product.ServiceType, out var serviceType)
            ? serviceType
            : throw new InvalidOperationException($"Service product '{approval.Product.Code}' maps to an unknown service type.");

    private static ServiceProduct ProductOf(SelfBookingApproval approval) =>
        WireEnum.TryParse<ServiceProduct>(approval.Product.Code, out var product)
            ? product
            : throw new InvalidOperationException($"Service product '{approval.Product.Code}' is not a known product code.");
}
