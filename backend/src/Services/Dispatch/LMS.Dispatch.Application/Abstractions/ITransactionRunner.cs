namespace LMS.Dispatch.Application.Abstractions;

public interface ITransactionRunner
{
    // 在同一个本地短事务内执行 work。遇到瞬态错误时 work 可能被重新调用，因此其中不得产生外部副作用。
    Task<TResult> ExecuteAsync<TResult>(
        Func<ITransactionContext, CancellationToken, Task<TResult>> work,
        CancellationToken cancellationToken);
}
