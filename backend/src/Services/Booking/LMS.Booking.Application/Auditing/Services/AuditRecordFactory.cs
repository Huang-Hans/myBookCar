using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Auditing;

namespace LMS.Booking.Application.Auditing.Services;

// 构造 Booking 本地的成功审计记录。变更摘要只记录字段名或固定短语，不复制字段值（避免个人信息进入审计）。
internal static class AuditRecordFactory
{
    private const string ServiceName = "booking";

    public static AuditRecord Updated(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        IReadOnlyList<string> changedFields,
        string? propertyId) =>
        Succeeded(context, operation, resourceType, resourceId, occurredAt, $"updated fields: {string.Join(',', changedFields)}", propertyId, null);

    public static AuditRecord Created(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        string? propertyId) =>
        Succeeded(context, operation, resourceType, resourceId, occurredAt, "created", propertyId, null);

    public static AuditRecord Succeeded(
        CommandContext context,
        string operation,
        string resourceType,
        string resourceId,
        DateTimeOffset occurredAt,
        string changeSummary,
        string? propertyId,
        QualificationAuditContext? qualification) =>
        new(
            AuditRecord.NewId(),
            context.Actor,
            operation,
            resourceType,
            resourceId,
            occurredAt,
            AuditOutcome.Succeeded,
            context.TraceId,
            ServiceName,
            changeSummary,
            resourceId,
            propertyId,
            Qualification: qualification);
}
