using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Idempotency.Models;
using LMS.Dispatch.Application.Notifications.Services;
using LMS.Dispatch.Application.Offers.Commands;
using LMS.Dispatch.Application.Offers.Options;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Offers.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Offers;
using Microsoft.Extensions.Options;

namespace LMS.Dispatch.Application.Offers.Handlers;

// 调度员为待分配行程向指定司机创建一轮邀请（客服派单入口与内部端点共用）。
// 行程摘要由 Booking 权威读取，不接受调用方自带；司机须启用、同物业且 AVAILABLE；同一行程、同一司机各自只能有一张占用中的邀请。
// 重试同一个请求（相同幂等键）返回已创建的邀请，不受“已有进行中的邀请”影响。
internal sealed class CreateDriverOfferHandler(
    CreateWorkflow workflow,
    IDriverRepository drivers,
    IDriverOfferRepository offers,
    IBookingAssignments bookings,
    OfferCreationGuard guard,
    OfferReplayProbe replays,
    OfferNotificationRecorder notifications,
    IOptions<OfferOptions> options,
    TimeProvider timeProvider) : ICreateDriverOfferHandler
{
    private const string Operation = "driver-offer.create";
    private const string ResourceType = "driver-offer";

    public async Task<OperationResult<DriverOffer>> HandleAsync(CreateDriverOfferCommand command, CancellationToken cancellationToken)
    {
        var scope = IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey);
        if (await replays.IsKnownAsync(scope, command.ToCanonicalForm(), cancellationToken))
        {
            return await workflow.ExecuteAsync(Specification(command, scope, NotRebuilt), cancellationToken);
        }

        return await CreateNewAsync(command, scope, cancellationToken);
    }

    private async Task<OperationResult<DriverOffer>> CreateNewAsync(CreateDriverOfferCommand command, IdempotencyScope scope, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(command.DriverId, command.Context.Scope, cancellationToken);
        if (driver is null)
        {
            return OperationResult<DriverOffer>.Failed(OperationOutcome.NotFound);
        }

        var trip = await bookings.GetAssignableTripAsync(command.TripId, cancellationToken);
        var rejection = guard.CheckTrip(command, trip)
            ?? guard.CheckDriver(driver, trip.Value!)
            ?? await guard.CheckShiftAsync(driver, trip.Value!, cancellationToken)
            ?? await guard.CheckOfferStateAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(
            Specification(command, scope, (_, token) => BuildAsync(command, driver, trip.Value!, token)),
            cancellationToken);
    }

    private async Task<DriverOffer> BuildAsync(CreateDriverOfferCommand command, Driver driver, AssignableTrip trip, CancellationToken cancellationToken)
    {
        var latest = await offers.FindLatestForTripAsync(trip.TripId, cancellationToken);

        return DriverOffer.Create(
            trip.TripId, trip.TripReference, driver.DriverId, trip.PropertyId,
            new OfferTripSummary(trip.PickupAt, trip.PickupName, trip.DropoffName, trip.Passengers, trip.VehicleClass),
            options.Value.Validity, timeProvider.GetUtcNow(), (latest?.Round ?? 0) + 1, driver.Name, command.Context.Actor);
    }

    private CreateSpecification<DriverOffer> Specification(
        CreateDriverOfferCommand command,
        IdempotencyScope scope,
        Func<ITransactionContext, CancellationToken, Task<DriverOffer>> build) =>
        new(
            scope,
            command.ToCanonicalForm(),
            build,
            offer => offer.OfferId,
            (offer, context, token) => InsertAndRecordAsync(offer, command.Context.TraceId, context, token),
            (id, token) => offers.FindByIdAsync(id, token),
            (offer, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, offer.OfferId, now, offer.PropertyId));

    // 邀请与“新邀请”实时通知事件在同一个事务内提交；幂等重放不会走到这里，因此不会重复登记。
    private async Task InsertAndRecordAsync(DriverOffer offer, string traceId, ITransactionContext context, CancellationToken cancellationToken)
    {
        await offers.InsertAsync(offer, context, cancellationToken);
        await notifications.RecordCreatedAsync(offer, traceId, context, cancellationToken);
    }

    // 重放路径上记录已存在，工作流不会再构建邀请；万一记录恰好过期而走到这里，宁可失败也不凭空创建。
    private static Task<DriverOffer> NotRebuilt(ITransactionContext context, CancellationToken cancellationToken) =>
        Task.FromException<DriverOffer>(new InvalidOperationException("The idempotency record expired while the request was being replayed."));
}
