using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Querying;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Application.History.Ports;
using LMS.Notification.Domain.History;
using LMS.Notification.Infrastructure.Documents;
using LMS.Notification.Infrastructure.Persistence;
using MongoDB.Driver;

namespace LMS.Notification.Infrastructure.Repositories;

internal sealed class MongoNotificationHistoryReader(IMongoDatabase database) : INotificationHistoryReader
{
    private static readonly string[] SearchFields = ["recipient", "channel", "eventType", "status", "contentPreview"];

    private static readonly IReadOnlyDictionary<string, string> SortFields = new Dictionary<string, string>
    {
        ["notificationId"] = "_id",
        ["recipient"] = "recipient",
        ["channel"] = "channel",
        ["occurredAt"] = "occurredAt",
        ["eventType"] = "eventType",
        ["status"] = "status"
    };

    private IMongoCollection<NotificationHistoryDocument> Collection =>
        database.GetCollection<NotificationHistoryDocument>(NotificationCollections.NotificationHistory);

    public async Task<PageResult<NotificationHistoryEntry>> ListAsync(ListNotificationHistoryQuery query, CancellationToken cancellationToken)
    {
        var page = await MongoPagedQuery.FindPageAsync(
            Collection,
            BuildFilter(query),
            MongoSortFactory.Create<NotificationHistoryDocument>(query.List.Sort, SortFields),
            query.List,
            cancellationToken);

        return new PageResult<NotificationHistoryEntry>(page.Items.Select(document => document.ToDomain()).ToList(), page.TotalItems);
    }

    public async Task<NotificationHistoryEntry?> FindAsync(string notificationId, CancellationToken cancellationToken)
    {
        var document = await Collection.Find(candidate => candidate.NotificationId == notificationId).FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    private static FilterDefinition<NotificationHistoryDocument> BuildFilter(ListNotificationHistoryQuery query)
    {
        var filter = Builders<NotificationHistoryDocument>.Filter;

        return new MongoFilterBuilder<NotificationHistoryDocument>(FilterDefinition<NotificationHistoryDocument>.Empty)
            .When(query.Status is not null, () => filter.Eq(candidate => candidate.Status, WireEnum.ToWire(query.Status!.Value)))
            .When(query.From is not null, () => filter.Gte(candidate => candidate.OccurredAt, UtcDateTimes.ToStored(query.From!.Value)))
            .When(query.To is not null, () => filter.Lt(candidate => candidate.OccurredAt, UtcDateTimes.ToStored(query.To!.Value)))
            .When(query.List.HasSearch, () => MongoTextSearch.Contains<NotificationHistoryDocument>(SearchFields, query.List.Search))
            .Build();
    }
}
