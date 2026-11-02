using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.AuditRecords.Models;
using LMS.Platform.Application.AuditRecords.Ports;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.AuditRecords.Handlers;

internal sealed class AuditRecordQueryHandler(IAuditRecordReader reader) : IAuditRecordQueryHandler
{
    public async Task<OperationResult<PageResult<AuditRecord>>> ListAsync(
        ListAuditRecordsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<AuditRecord>>.Failed(OperationOutcome.Forbidden);
        }

        if (query.From is not null && query.To is not null && query.From >= query.To)
        {
            return OperationResult<PageResult<AuditRecord>>.Invalid([new FieldIssue("to", "Must be later than from")]);
        }

        return OperationResult<PageResult<AuditRecord>>.Succeeded(await reader.ListAsync(query, cancellationToken));
    }
}
