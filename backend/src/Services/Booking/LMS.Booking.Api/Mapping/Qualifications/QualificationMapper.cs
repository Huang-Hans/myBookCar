using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Booking.Api.Contracts.Qualifications;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Auditing;
using LMS.Booking.Domain.Qualifications;

namespace LMS.Booking.Api.Mapping.Qualifications;

internal static class QualificationMapper
{
    public static ListQualificationsQuery ToListQuery(QualificationListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.Status);

    public static ListQualificationAuditsQuery ToAuditListQuery(QualificationAuditListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId);

    public static QualificationView ToView(QualificationReference reference) => new(
        reference.QualificationId,
        new GuestSnapshotView(reference.Guest.GuestId, reference.Guest.Name, reference.Guest.TierCode),
        reference.PropertyId,
        reference.LoyaltyOutcome,
        reference.QualificationReferenceCode,
        reference.EntitlementType,
        reference.PointsBalance,
        reference.PointsRequired,
        reference.ApprovalChain,
        WireEnum.ToWire(reference.Status),
        reference.Justification,
        reference.OperatorId,
        reference.OperatedAt?.UtcDateTime,
        reference.Version,
        reference.Request is null
            ? null
            : new QualificationRequestView(
                reference.Request.RequestedBy, reference.Request.RequestedByName, reference.Request.RequestedByRole,
                reference.Request.Reason, reference.Request.RequestedAt.UtcDateTime),
        DecisionOf(reference));

    // 批准 / 拒绝时，最近一次操作就是决定；其余状态没有决定。
    private static QualificationDecisionView? DecisionOf(QualificationReference reference) =>
        reference.Status is QualificationStatus.Approved or QualificationStatus.Rejected && reference.OperatorId is not null && reference.OperatedAt is not null
            ? new QualificationDecisionView(reference.OperatorId, reference.OperatedAt.Value.UtcDateTime, reference.Justification)
            : null;

    public static QualificationAuditView ToAuditView(AuditRecord record) => new(
        record.AuditId,
        record.Actor,
        record.Operation,
        record.ResourceType,
        record.ResourceId,
        record.OccurredAt.UtcDateTime,
        record.Outcome.ToString(),
        record.TraceId,
        record.SourceService,
        record.ChangeSummary,
        record.ReasonReference,
        record.PropertyId,
        record.Device,
        record.SourceIp,
        record.SessionReference,
        record.Qualification is null
            ? null
            : new QualificationAuditContextView(
                record.Qualification.GuestId, record.Qualification.Justification, WireEnum.ToWire(record.Qualification.Status)));

    public static RequestParseResult<CreateQualificationRequestCommand> ToCreateCommand(
        CreateQualificationRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateQualificationRequestCommand(
            errors.RequiredText("guestId", request.GuestId),
            errors.RequiredText("propertyId", request.PropertyId),
            errors.RequiredText("justification", request.Justification),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateQualificationRequestCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateQualificationRequestCommand>.Success(command);
    }

    public static RequestParseResult<QualificationExceptionCommand> ToCommand(
        string qualificationId,
        long expectedVersion,
        QualificationJustificationRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var justification = errors.RequiredText("justification", request.Justification);

        return errors.HasErrors
            ? RequestParseResult<QualificationExceptionCommand>.Fail(errors.ToFailure())
            : RequestParseResult<QualificationExceptionCommand>.Success(
                new QualificationExceptionCommand(qualificationId, expectedVersion, justification, idempotencyKey, context));
    }
}
