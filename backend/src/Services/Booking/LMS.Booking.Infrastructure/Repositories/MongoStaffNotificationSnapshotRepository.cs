using LMS.BuildingBlocks.Core.Notifications;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Notifications.Models;
using LMS.Booking.Application.Notifications.Ports;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

// 客服通知快照：只有带来源元数据的 BOOKING_CREATED 站内通知，且订单物业仍在调用者当前授权范围内。
// 旧的审批、取消等通知没有 eventType，因此不会进入这个入口；已离开物业范围的收件人读不到业务内容。
internal sealed class MongoStaffNotificationSnapshotRepository(IMongoDatabase database) : IStaffNotificationSnapshotRepository
{
    private static readonly string BookingCreated = WireEnum.ToWire(RealtimeEventType.BookingCreated);

    private IMongoCollection<ConsoleNotificationDocument> Collection =>
        database.GetCollection<ConsoleNotificationDocument>(BookingCollections.ConsoleNotifications);

    public async Task<StaffNotificationSnapshot> ReadAsync(StaffNotificationSnapshotQuery query, CancellationToken cancellationToken)
    {
        var visible = VisibleTo(query);
        var unreadFilter = Builders<ConsoleNotificationDocument>.Filter.And(visible, Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.IsRead, false));
        var total = await Collection.CountDocumentsAsync(visible, cancellationToken: cancellationToken);
        var unread = await Collection.CountDocumentsAsync(unreadFilter, cancellationToken: cancellationToken);
        var items = await Collection
            .Find(visible)
            .Sort(Builders<ConsoleNotificationDocument>.Sort.Descending(candidate => candidate.OccurredAt).Ascending(candidate => candidate.NotificationId))
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(cancellationToken);

        return new StaffNotificationSnapshot(items.Select(document => document.ToDomain()).ToList(), total, unread);
    }

    private static FilterDefinition<ConsoleNotificationDocument> VisibleTo(StaffNotificationSnapshotQuery query)
    {
        var filter = Builders<ConsoleNotificationDocument>.Filter;
        var own = filter.And(
            filter.Eq(candidate => candidate.RecipientUserId, query.RecipientUserId),
            filter.Eq(candidate => candidate.EventType, BookingCreated));

        return query.Scope.IsAllProperties ? own : filter.And(own, filter.Eq(candidate => candidate.PropertyId, query.Scope.PropertyId));
    }
}
