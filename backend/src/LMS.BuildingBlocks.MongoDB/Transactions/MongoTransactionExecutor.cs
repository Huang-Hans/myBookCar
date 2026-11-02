using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Transactions;

// 快照读、majority 写、primary 读。事务要求部署为副本集。
internal sealed class MongoTransactionExecutor(IMongoClient client) : IMongoTransactionExecutor
{
    private static readonly TransactionOptions TransactionOptions = new(
        ReadConcern.Snapshot,
        ReadPreference.Primary,
        WriteConcern.WMajority);

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<IClientSessionHandle, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        using var session = await client.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(operation, TransactionOptions, cancellationToken);
    }
}
