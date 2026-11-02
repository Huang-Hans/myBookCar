using LMS.BuildingBlocks.Core.Notifications.Delivery;
using LMS.BuildingBlocks.Core.Notifications.Events;
using LMS.BuildingBlocks.Core.Wire;
using MongoDB.Driver;

namespace LMS.BuildingBlocks.Realtime.Mongo;

// 待发送事件的存储。登记与扇出物化在调用方的本地事务内完成（带 session 的重载）；
// 后台领取使用条件更新和有期限租约，处理结果按租约持有者条件写回，租约被他人接管后迟到的写入不生效。
public sealed class MongoNotificationOutbox(IMongoDatabase database) : INotificationOutbox
{
    private static readonly string Pending = WireEnum.ToWire(NotificationEventState.Pending);

    private IMongoCollection<NotificationEventDocument> Collection =>
        database.GetCollection<NotificationEventDocument>(NotificationEventCollection.Name);

    public Task InsertAsync(
        IClientSessionHandle session,
        NotificationEventDraft draft,
        string? notificationId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        Collection.InsertOneAsync(session, NotificationEventDocument.FromDraft(draft, notificationId, now), cancellationToken: cancellationToken);

    public async Task<IReadOnlySet<string>> FindExistingIdsAsync(
        IClientSessionHandle session,
        IReadOnlyCollection<string> eventIds,
        CancellationToken cancellationToken)
    {
        var found = await Collection
            .Find(session, Builders<NotificationEventDocument>.Filter.In(candidate => candidate.EventId, eventIds))
            .Project(candidate => candidate.EventId)
            .ToListAsync(cancellationToken);

        return found.ToHashSet(StringComparer.Ordinal);
    }

    // 源事件物化完成：只有仍由本租约持有者领取的 Pending 源事件才会被置为 Materialized，否则整个物化事务应回滚。
    public async Task<bool> MarkMaterializedAsync(
        IClientSessionHandle session,
        string eventId,
        string leaseOwner,
        DateTimeOffset now,
        DateTimeOffset purgeAt,
        CancellationToken cancellationToken)
    {
        var update = Terminal(NotificationEventState.Materialized, purgeAt).Set(candidate => candidate.PublishedAt, now.UtcDateTime);
        var result = await Collection.UpdateOneAsync(session, Leased(eventId, leaseOwner), update, cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<IReadOnlyList<NotificationEvent>> ClaimDueAsync(
        string leaseOwner,
        DateTimeOffset now,
        TimeSpan lease,
        int take,
        CancellationToken cancellationToken)
    {
        var claimed = new List<NotificationEvent>();
        while (claimed.Count < take && await ClaimOneAsync(leaseOwner, now, lease, cancellationToken) is { } document)
        {
            claimed.Add(document.ToDomain());
        }

        return claimed;
    }

    public Task<bool> MarkPublishedAsync(string eventId, string leaseOwner, DateTimeOffset now, DateTimeOffset purgeAt, CancellationToken cancellationToken) =>
        UpdateLeasedAsync(
            eventId, leaseOwner, Terminal(NotificationEventState.Published, purgeAt).Set(candidate => candidate.PublishedAt, now.UtcDateTime), cancellationToken);

    public Task<bool> MarkSkippedAsync(string eventId, string leaseOwner, string reason, DateTimeOffset purgeAt, CancellationToken cancellationToken) =>
        UpdateLeasedAsync(
            eventId, leaseOwner, Terminal(NotificationEventState.Skipped, purgeAt).Set(candidate => candidate.FailureCategory, reason), cancellationToken);

    public Task<bool> MarkFailedAsync(string eventId, string leaseOwner, string reason, DateTimeOffset purgeAt, CancellationToken cancellationToken) =>
        UpdateLeasedAsync(
            eventId, leaseOwner, Terminal(NotificationEventState.Failed, purgeAt).Set(candidate => candidate.FailureCategory, reason), cancellationToken);

    public Task<bool> RescheduleAsync(
        string eventId,
        string leaseOwner,
        int attempts,
        DateTimeOffset nextAttemptAt,
        string reason,
        CancellationToken cancellationToken) =>
        UpdateLeasedAsync(
            eventId,
            leaseOwner,
            Builders<NotificationEventDocument>.Update
                .Set(candidate => candidate.Attempts, attempts)
                .Set(candidate => candidate.NextAttemptAt, nextAttemptAt.UtcDateTime)
                .Set(candidate => candidate.FailureCategory, reason)
                .Set(candidate => candidate.LeaseOwner, null)
                .Set(candidate => candidate.LeaseUntil, null),
            cancellationToken);

    private Task<NotificationEventDocument?> ClaimOneAsync(string leaseOwner, DateTimeOffset now, TimeSpan lease, CancellationToken cancellationToken)
    {
        var filter = Builders<NotificationEventDocument>.Filter;
        var due = filter.And(
            filter.Eq(candidate => candidate.State, Pending),
            filter.Lte(candidate => candidate.NextAttemptAt, now.UtcDateTime),
            filter.Or(filter.Eq(candidate => candidate.LeaseUntil, null), filter.Lte(candidate => candidate.LeaseUntil, now.UtcDateTime)));
        var update = Builders<NotificationEventDocument>.Update
            .Set(candidate => candidate.LeaseOwner, leaseOwner)
            .Set(candidate => candidate.LeaseUntil, (now + lease).UtcDateTime);
        var options = new FindOneAndUpdateOptions<NotificationEventDocument>
        {
            Sort = Builders<NotificationEventDocument>.Sort.Ascending(candidate => candidate.NextAttemptAt),
            ReturnDocument = ReturnDocument.After
        };

        return Collection.FindOneAndUpdateAsync(due, update, options, cancellationToken)!;
    }

    private async Task<bool> UpdateLeasedAsync(
        string eventId,
        string leaseOwner,
        UpdateDefinition<NotificationEventDocument> update,
        CancellationToken cancellationToken)
    {
        var result = await Collection.UpdateOneAsync(Leased(eventId, leaseOwner), update, cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    private static FilterDefinition<NotificationEventDocument> Leased(string eventId, string leaseOwner)
    {
        var filter = Builders<NotificationEventDocument>.Filter;

        return filter.And(
            filter.Eq(candidate => candidate.EventId, eventId),
            filter.Eq(candidate => candidate.State, Pending),
            filter.Eq(candidate => candidate.LeaseOwner, leaseOwner));
    }

    private static UpdateDefinition<NotificationEventDocument> Terminal(NotificationEventState state, DateTimeOffset purgeAt) =>
        Builders<NotificationEventDocument>.Update
            .Set(candidate => candidate.State, WireEnum.ToWire(state))
            .Set(candidate => candidate.PurgeAt, purgeAt.UtcDateTime)
            .Set(candidate => candidate.LeaseOwner, null)
            .Set(candidate => candidate.LeaseUntil, null);
}
