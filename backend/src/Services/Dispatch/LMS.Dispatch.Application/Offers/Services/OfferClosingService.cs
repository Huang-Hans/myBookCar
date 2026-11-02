using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Notifications.Services;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Services;

// 把邀请以版本为条件写成关闭态（拒绝 / 过期 / 被取代），并与审计同事务提交。返回 false 表示邀请已被并发改变。
internal sealed class OfferClosingService(
    ITransactionRunner transactionRunner,
    IDriverOfferRepository offers,
    IAuditRecordRepository auditRecords,
    OfferNotificationRecorder events,
    TimeProvider timeProvider)
{
    private const string Operation = "driver-offer.close";
    private const string ResourceType = "driver-offer";

    public Task<bool> CloseAsync(DriverOffer current, DriverOffer closed, CommandContext context, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync(
            async (transaction, token) =>
            {
                if (!await offers.TryReplaceAsync(closed, current.Version, transaction, token))
                {
                    return false;
                }

                await auditRecords.InsertAsync(
                    AuditRecordFactory.Succeeded(
                        context, Operation, ResourceType, closed.OfferId, timeProvider.GetUtcNow(),
                        $"status: {WireEnum.ToWire(closed.Status)}", closed.PropertyId),
                    transaction,
                    token);
                await events.RecordClosedAsync(closed, context.TraceId, transaction, token);

                return true;
            },
            cancellationToken);
}
