namespace LMS.Dispatch.Application.Abstractions;

// 事务上下文的不透明标记。Application 只负责把它传给仓储，具体内容由 Infrastructure 解释，
// 这样 Application 不依赖任何数据库类型。
public interface ITransactionContext
{
}
