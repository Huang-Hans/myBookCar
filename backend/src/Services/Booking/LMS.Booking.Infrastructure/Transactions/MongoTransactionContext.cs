using LMS.Booking.Application.Abstractions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Transactions;

// 一次本地事务独占的会话。不共享给并行事务或其他客户端。
internal sealed class MongoTransactionContext(IClientSessionHandle session) : ITransactionContext
{
    public IClientSessionHandle Session { get; } = session;

    public static IClientSessionHandle RequireSession(ITransactionContext context) =>
        context is MongoTransactionContext mongoContext
            ? mongoContext.Session
            : throw new InvalidOperationException("The transaction context was not created by the MongoDB transaction runner.");
}
