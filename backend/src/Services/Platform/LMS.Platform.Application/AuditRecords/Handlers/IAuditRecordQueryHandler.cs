using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.AuditRecords.Models;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.AuditRecords.Handlers;

public interface IAuditRecordQueryHandler
{
    Task<OperationResult<PageResult<AuditRecord>>> ListAsync(ListAuditRecordsQuery query, CancellationToken cancellationToken);
}
