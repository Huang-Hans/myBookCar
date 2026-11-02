using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Application.ConsoleNotifications.Models;
using LMS.Booking.Application.ConsoleNotifications.Ports;
using LMS.Booking.Domain.Notifications;
using LMS.Booking.Infrastructure.Documents;
using LMS.Booking.Infrastructure.Persistence;
using LMS.Booking.Infrastructure.Transactions;
using MongoDB.Driver;

namespace LMS.Booking.Infrastructure.Repositories;

internal sealed class MongoConsoleNotificationRepository(IMongoDatabase database) : IConsoleNotificationRepository
{
    private static readonly string[] SearchFields = ["summary", "tripReference"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["notificationId"] = "_id",
        ["occurredAt"] = "occurredAt",
        ["type"] = "type",
        ["summary"] = "summary",
        ["tripReference"] = "tripReference"
    };

    private IMongoCollection<ConsoleNotificationDocument> Collection =>
        database.GetCollection<ConsoleNotificationDocument>(BookingCollections.ConsoleNotifications);

    public async Task<PageResult<ConsoleNotification>> ListAsync(ListConsoleNotificationsQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<ConsoleNotificationDocument>.Filter;
        var combined = new MongoFilterBuilder<ConsoleNotificationDocument>(
                Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.RecipientUserId, query.RecipientUserId))
            .When(query.Type is not null, () => filter.Eq(candidate => candidate.Type, WireEnum.ToWire(query.Type!.Value)))
            .When(query.UnreadOnly, () => filter.Eq(candidate => candidate.IsRead, false))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<ConsoleNotificationDocument>(SearchFields, query.List.Search))
            .Build();
        var page = await MongoPagedQuery.FindPageAsync(
            Collection, combined, MongoSortFactory.Create<ConsoleNotificationDocument>(query.List.Sort, SortFields), query.List, cancellationToken);

        return new PageResult<ConsoleNotification>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<long> CountUnreadAsync(string recipientUserId, CancellationToken cancellationToken)
    {
        var filter = Builders<ConsoleNotificationDocument>.Filter.And(
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.RecipientUserId, recipientUserId),
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.IsRead, false));

        return await Collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<bool> TryMarkReadAsync(string notificationId, string recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var filter = Builders<ConsoleNotificationDocument>.Filter.And(
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.NotificationId, notificationId),
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.RecipientUserId, recipientUserId));
        var update = Builders<ConsoleNotificationDocument>.Update
            .Set(candidate => candidate.IsRead, true)
            .Set(candidate => candidate.ReadAt, readAt.UtcDateTime);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<long> MarkAllReadAsync(string recipientUserId, DateTimeOffset readAt, CancellationToken cancellationToken)
    {
        var filter = Builders<ConsoleNotificationDocument>.Filter.And(
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.RecipientUserId, recipientUserId),
            Builders<ConsoleNotificationDocument>.Filter.Eq(candidate => candidate.IsRead, false));
        var update = Builders<ConsoleNotificationDocument>.Update
            .Set(candidate => candidate.IsRead, true)
            .Set(candidate => candidate.ReadAt, readAt.UtcDateTime);
        var result = await Collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);

        return result.ModifiedCount;
    }

    public Task InsertAsync(ConsoleNotification notification, ITransactionContext context, CancellationToken cancellationToken) =>
        MongoInsert.OneAsync(
            Collection,
            MongoTransactionContext.RequireSession(context),
            ConsoleNotificationDocument.FromDomain(notification),
            cancellationToken);
}
