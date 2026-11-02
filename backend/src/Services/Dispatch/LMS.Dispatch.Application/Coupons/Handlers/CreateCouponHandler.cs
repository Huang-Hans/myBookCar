using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Application.Coupons.Ports;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Idempotency.Ports;
using LMS.Dispatch.Application.Idempotency.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Handlers;

// 新增優惠券。券碼全庫唯一，並且必須綁定一名會員。全物業與限定車型只允許全物業授權建立。
internal sealed class CreateCouponHandler(
    CreateWorkflow workflow,
    ICouponRepository coupons,
    IPlatformDirectory directory,
    IIdempotencyRecordRepository idempotencyRecords,
    BusinessCalendar calendar,
    TimeProvider timeProvider) : ICreateCouponHandler
{
    private const string Operation = "coupon.create";
    private const string ResourceType = "coupon";

    public async Task<OperationResult<CouponCard>> HandleAsync(CreateCouponCommand command, CancellationToken cancellationToken)
    {
        Coupon? candidate = null;
        if (!await IsReplayAsync(command, cancellationToken))
        {
            var prepared = await PrepareAsync(command, cancellationToken);
            if (prepared.Failure is not null)
            {
                return prepared.Failure;
            }

            candidate = prepared.Coupon;
        }

        var created = await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
        if (!created.IsSuccess)
        {
            return OperationResult<CouponCard>.FailureOf(created);
        }

        var card = new CouponCard(created.Value!, calendar.Today());

        return created.Outcome == OperationOutcome.Replayed
            ? OperationResult<CouponCard>.Replayed(card)
            : OperationResult<CouponCard>.Created(card);
    }

    private async Task<bool> IsReplayAsync(CreateCouponCommand command, CancellationToken cancellationToken)
    {
        var fingerprint = IdempotencyFingerprintFactory.Create(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm());

        return await idempotencyRecords.FindActiveAsync(fingerprint, timeProvider.GetUtcNow(), cancellationToken) is not null;
    }

    private async Task<(OperationResult<CouponCard>? Failure, Coupon? Coupon)> PrepareAsync(
        CreateCouponCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Scope != CouponScope.Property && !command.Context.Scope.IsAllProperties)
        {
            return (OperationResult<CouponCard>.Failed(OperationOutcome.Forbidden), null);
        }

        if (command.Scope == CouponScope.Property)
        {
            var property = await DriverPropertyCheck.CheckAsync<CouponCard>(directory, command.Context.Scope, command.PropertyId!, cancellationToken);
            if (property is not null)
            {
                return (property, null);
            }
        }

        if (await coupons.FindByCodeAsync(command.Code, cancellationToken) is not null)
        {
            return (OperationResult<CouponCard>.Invalid([new FieldIssue("code", "Already exists")]), null);
        }

        var candidate = Coupon.Create(
            command.Code,
            command.Name,
            command.MemberId,
            command.Type,
            command.FaceAmount,
            command.Scope,
            command.PropertyId,
            command.VehicleRestriction,
            command.ValidFrom,
            command.ValidTo,
            command.IssuedQuantity,
            command.Disabled);
        var issues = CouponRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return (OperationResult<CouponCard>.Invalid(issues), null);
        }

        return (null, candidate);
    }

    private CreateSpecification<Coupon> BuildSpecification(CreateCouponCommand command, Coupon? candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate!),
            coupon => coupon.CouponId,
            (coupon, context, token) => coupons.InsertAsync(coupon, context, token),
            (id, token) => coupons.FindByIdAsync(id, token),
            (coupon, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, coupon.CouponId, now, coupon.PropertyId));
}
