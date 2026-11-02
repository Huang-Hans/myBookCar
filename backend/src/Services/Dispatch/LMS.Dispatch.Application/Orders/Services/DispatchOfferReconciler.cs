using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Offers.Ports;
using LMS.Dispatch.Application.Offers.Services;
using LMS.Dispatch.Domain.Offers;
using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Orders.Services;

// 读取订单详情时，把仍占用中的邀请与 Booking 的权威事实对齐，使“处理中”的邀请和失效的邀请不会无限占住行程与司机：
// Booking 已按本邀请指派（接受后响应丢失）则补完 ACCEPTED 与司机 ON_TRIP；订单已被他人指派、已取消或已完成则把待响应邀请关闭为 SUPERSEDED。
// 订单尚未指派而邀请处于处理中时不动：必须等司机用同一邀请重试，不能直接改派他人。
internal sealed class DispatchOfferReconciler(
    IDriverOfferRepository offers,
    OfferClosingService closing,
    OfferAcceptanceCommitter committer,
    TimeProvider timeProvider)
{
    public async Task<DriverOffer?> ReconcileAsync(BookingOrder order, DriverOffer? latest, CommandContext context, CancellationToken cancellationToken)
    {
        if (latest is not { IsOpen: true })
        {
            return latest;
        }

        if (order.Assignment is { } assignment)
        {
            return await ReconcileAssignedAsync(assignment, latest, context, cancellationToken);
        }

        return order.Status is OrderStatus.Cancelled or OrderStatus.Completed && latest.Status == DriverOfferStatus.Pending
            ? await SupersedeAsync(latest, context, cancellationToken)
            : latest;
    }

    private async Task<DriverOffer?> ReconcileAssignedAsync(
        BookingOrderAssignment assignment,
        DriverOffer latest,
        CommandContext context,
        CancellationToken cancellationToken)
    {
        if (latest.Status == DriverOfferStatus.Accepting && assignment.OfferId == latest.OfferId)
        {
            await committer.CommitAsync(latest, context, cancellationToken);

            return await offers.FindByIdAsync(latest.OfferId, cancellationToken);
        }

        return await SupersedeAsync(latest, context, cancellationToken);
    }

    private async Task<DriverOffer?> SupersedeAsync(DriverOffer latest, CommandContext context, CancellationToken cancellationToken)
    {
        await closing.CloseAsync(latest, latest.Superseded(timeProvider.GetUtcNow()), context, cancellationToken);

        return await offers.FindByIdAsync(latest.OfferId, cancellationToken);
    }
}
