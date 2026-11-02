using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Notifications.Services;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Services;

// Booking 已权威指派后，在同一本地事务内把邀请写成 ACCEPTED，并把司机置为 ON_TRIP。
// 返回 false 表示邀请已被并发改变（例如同一邀请的重试已先提交），或司机主档在提交期间被修改（整体回滚，邀请保持处理中）。
internal sealed class OfferAcceptanceCommitter(
    ITransactionRunner transactionRunner,
    IDriverOfferRepository offers,
    IDriverRepository drivers,
    IAuditRecordRepository auditRecords,
    OfferNotificationRecorder events,
    TimeProvider timeProvider)
{
    private const string Operation = "driver-offer.accept";
    private const string ResourceType = "driver-offer";

    public async Task<bool> CommitAsync(DriverOffer accepting, CommandContext context, CancellationToken cancellationToken)
    {
        try
        {
            return await transactionRunner.ExecuteAsync(
                (transaction, token) => CommitInTransactionAsync(accepting, context, transaction, token),
                cancellationToken);
        }
        catch (DriverStateChangedException)
        {
            return false;
        }
    }

    private async Task<bool> CommitInTransactionAsync(
        DriverOffer accepting,
        CommandContext context,
        ITransactionContext transaction,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (!await offers.TryReplaceAsync(accepting.Accepted(now), accepting.Version, transaction, cancellationToken))
        {
            return false;
        }

        var driver = await drivers.FindVisibleAsync(accepting.DriverId, PropertyScope.AllProperties, transaction, cancellationToken);
        if (driver is not null)
        {
            var onTrip = driver.WithAvailability(DriverAvailability.OnTrip, DriverAvailabilitySources.TripAssignment, now);
            if (!await drivers.TryReplaceAsync(onTrip, driver.Version, transaction, cancellationToken))
            {
                throw new DriverStateChangedException();
            }
        }

        await auditRecords.InsertAsync(
            AuditRecordFactory.Succeeded(context, Operation, ResourceType, accepting.OfferId, now, "accepted; driver on trip", accepting.PropertyId),
            transaction,
            cancellationToken);
        await events.RecordAcceptedAsync(accepting, context.TraceId, transaction, cancellationToken);

        return true;
    }
}
