using MongoDB.Driver;

namespace LMS.BuildingBlocks.MongoDB.Transactions;

public interface IMongoTransactionExecutor
{
    // 在独立会话上的一个本地短事务内执行操作。遇到瞬态错误时操作可能被重新调用，
    // 因此其中不得产生外部副作用。
    Task<TResult> ExecuteAsync<TResult>(
        Func<IClientSessionHandle, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);
}
