using LMS.Platform.Api.Contracts.Audit;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Api.Mapping.Audit;

internal static class AuditViewMapper
{
    // outcome 保持现有枚举字面值 Succeeded / Failed（合同 3.3.13）。
    public static AuditView ToView(AuditRecord record) => new(
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
        record.SessionReference);
}
