using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts.Batches;
using LMS.Dispatch.Infrastructure.Documents;
using LMS.Dispatch.Infrastructure.Persistence;
using LMS.Dispatch.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Dispatch.Infrastructure.Repositories;

internal sealed class MongoShiftBatchRepository(IMongoDatabase database) : IShiftBatchRepository
{
    private IMongoCollection<ShiftBatchDocument> Collection => database.GetCollection<ShiftBatchDocument>(DispatchCollections.ShiftBatches);

    public async Task<ShiftBatch?> FindAsync(string batchId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(batch => batch.BatchId == batchId).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public Task InsertAsync(ShiftBatch batch, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(Collection, MongoTransactionContext.RequireSession(context), ShiftBatchDocument.FromDomain(batch), cancellationToken);
}
