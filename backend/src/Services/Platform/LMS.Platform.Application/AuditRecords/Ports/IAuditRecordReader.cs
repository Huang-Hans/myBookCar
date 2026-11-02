using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.AuditRecords.Models;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.AuditRecords.Ports;

public interface IAuditRecordReader
{
    // 非全物业调用者只能看到带有其物业标识的记录；没有物业标识的全局管理操作只对全物业调用者可见。
    Task<PageResult<AuditRecord>> ListAsync(ListAuditRecordsQuery query, CancellationToken cancellationToken);
}
