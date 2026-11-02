using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Ports;

public interface IShiftBatchRepository
{
    Task<ShiftBatch?> FindAsync(string batchId, CancellationToken cancellationToken);
    Task InsertAsync(ShiftBatch batch, ITransactionContext context, CancellationToken cancellationToken);
}
