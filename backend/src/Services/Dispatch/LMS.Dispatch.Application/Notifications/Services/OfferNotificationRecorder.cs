using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Notifications.Ports;
using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Notifications.Services;

// 邀请相关用例在自身事务内登记司机实时通知事件的入口。只在业务事实真正首次提交的分支调用：
// 幂等重放、并发输家（条件写入失败）和 ACCEPTING 处理中都不调用，因此不会重复或提前产生事件。
internal sealed class OfferNotificationRecorder(INotificationEventRecorder recorder, TimeProvider timeProvider)
{
    public Task RecordCreatedAsync(DriverOffer offer, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(DispatchNotificationEvents.OfferCreated(offer, traceId), context, cancellationToken);

    public Task RecordClosedAsync(DriverOffer closed, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(DispatchNotificationEvents.OfferClosed(closed, traceId, timeProvider.GetUtcNow()), context, cancellationToken);

    public Task RecordAcceptedAsync(DriverOffer accepted, string traceId, ITransactionContext context, CancellationToken cancellationToken) =>
        recorder.RecordAsync(DispatchNotificationEvents.OfferAccepted(accepted, traceId, timeProvider.GetUtcNow()), context, cancellationToken);
}
