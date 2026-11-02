using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Handlers;

// 局部更新優惠券。改券碼時仍須全庫唯一。全物業與限定車型只允許全物業授權修改。
internal sealed class UpdateCouponHandler(
    PatchWorkflow workflow,
    ICouponRepository coupons,
    IPlatformDirectory directory,
    BusinessCalendar calendar) : IUpdateCouponHandler
{
    private const string Operation = "coupon.update";
    private const string ResourceType = "coupon";

    public async Task<OperationResult<CouponCard>> HandleAsync(UpdateCouponCommand command, CancellationToken cancellationToken)
    {
        var updated = await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
        if (!updated.IsSuccess)
        {
            return OperationResult<CouponCard>.FailureOf(updated);
        }

        return OperationResult<CouponCard>.Succeeded(new CouponCard(updated.Value!, calendar.Today()));
    }

    private PatchSpecification<Coupon> BuildSpecification(UpdateCouponCommand command) =>
        new(
            command.CouponId,
            command.ExpectedVersion,
            (id, context, token) => coupons.FindVisibleAsync(id, command.Context.Scope, context, token),
            coupon => coupon.Version,
            (coupon, context, token) => MergeAsync(coupon, command, context, token),
            (updated, expected, context, token) => coupons.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.CouponId, now, command.Changes.ChangedFields(), after.PropertyId));

    private async Task<OperationResult<Coupon>> MergeAsync(
        Coupon current,
        UpdateCouponCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var merged = current.Apply(command.Changes);
        var issues = CouponRules.Validate(merged);
        if (issues.Count > 0)
        {
            return OperationResult<Coupon>.Invalid(issues);
        }

        if (merged.Scope != CouponScope.Property && !command.Context.Scope.IsAllProperties)
        {
            return OperationResult<Coupon>.Failed(OperationOutcome.Forbidden);
        }

        if (merged.Scope == CouponScope.Property)
        {
            var property = await DriverPropertyCheck.CheckAsync<Coupon>(directory, command.Context.Scope, merged.PropertyId!, cancellationToken);
            if (property is not null)
            {
                return property;
            }
        }

        if (!string.Equals(current.Code, merged.Code, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await coupons.FindByCodeAsync(merged.Code, context, cancellationToken);
            if (existing is not null && existing.CouponId != current.CouponId)
            {
                return OperationResult<Coupon>.Invalid([new FieldIssue("code", "Already exists")]);
            }
        }

        return OperationResult<Coupon>.Succeeded(merged);
    }
}
