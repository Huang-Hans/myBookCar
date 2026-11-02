using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Api.Contracts.Audit;
using LMS.Platform.Application.AuditRecords.Models;

namespace LMS.Platform.Api.Mapping.Audit;

internal static class AuditRequestMapper
{
    public static ListAuditRecordsQuery ToListQuery(AuditRecordListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.From, request.To);
}
