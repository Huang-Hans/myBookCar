using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Domain.Auditing;

namespace LMS.Reporting.Application.Auditing.Ports;

public interface IAuditRecordRepository
{
    Task InsertAsync(AuditRecord record, ITransactionContext context, CancellationToken cancellationToken);
}
