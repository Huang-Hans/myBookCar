using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Qualifications.Models;
using LMS.Booking.Domain.Auditing;

namespace LMS.Booking.Application.Auditing.Ports;

public interface IQualificationAuditReader
{
    // 只返回带资格上下文的审计记录；非全物业调用者只看到自己物业的记录。
    Task<PageResult<AuditRecord>> ListAsync(ListQualificationAuditsQuery query, CancellationToken cancellationToken);
}
