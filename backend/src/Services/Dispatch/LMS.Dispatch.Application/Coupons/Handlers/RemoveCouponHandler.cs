using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Ports;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Handlers;

// 刪除優惠券。不可見與不存在都回 NotFound。全物業與限定車型只允許全物業授權刪除，否則 403。
internal sealed class RemoveCouponHandler(
    ITransactionRunner transactions,
    ICouponRepository coupons,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider) : IRemoveCouponHandler
{
    private const string Operation = "coupon.remove";
    private const string ResourceType = "coupon";

    public Task<OperationResult<bool>> HandleAsync(RemoveCouponCommand command, CancellationToken cancellationToken) =>
        transactions.ExecuteAsync((context, token) => RemoveAsync(command, context, token), cancellationToken);

    private async Task<OperationResult<bool>> RemoveAsync(RemoveCouponCommand command, ITransactionContext context, CancellationToken cancellationToken)
    {
        var coupon = await coupons.FindVisibleAsync(command.CouponId, command.Context.Scope, context, cancellationToken);
        if (coupon is null)
        {
            return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        }

        if (coupon.Scope != CouponScope.Property && !command.Context.Scope.IsAllProperties)
        {
            return OperationResult<bool>.Failed(OperationOutcome.Forbidden);
        }

        if (!await coupons.DeleteAsync(coupon.CouponId, context, cancellationToken))
        {
            return OperationResult<bool>.Failed(OperationOutcome.NotFound);
        }

        await auditRecords.InsertAsync(
            AuditRecordFactory.Succeeded(command.Context, Operation, ResourceType, coupon.CouponId, timeProvider.GetUtcNow(), "removed", coupon.PropertyId),
            context,
            cancellationToken);

        return OperationResult<bool>.Succeeded(true);
    }
}
