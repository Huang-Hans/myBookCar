using LMS.BuildingBlocks.MongoDB.Transactions;
using LMS.Platform.Application.Abstractions;

namespace LMS.Platform.Infrastructure.Transactions;

internal sealed class MongoTransactionRunner(IMongoTransactionExecutor executor) : ITransactionRunner
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<ITransactionContext, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync((session, token) => work(new MongoTransactionContext(session), token), cancellationToken);
}
