using LMS.Dispatch.Domain.Shifts.Batches;
using LMS.Dispatch.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Dispatch.Infrastructure.Documents;

[BsonIgnoreExtraElements]
internal sealed class ShiftBatchDocument
{
    [BsonId] public string BatchId { get; init; } = string.Empty;
    [BsonElement("actor")] public string Actor { get; init; } = string.Empty;
    [BsonElement("createdAt"), BsonDateTimeOptions(Kind = DateTimeKind.Utc)] public DateTime CreatedAt { get; init; }
    [BsonElement("items")] public IReadOnlyList<ShiftBatchItemDocument> Items { get; init; } = [];

    public static ShiftBatchDocument FromDomain(ShiftBatch batch) => new()
    {
        BatchId = batch.BatchId, Actor = batch.Actor,
        CreatedAt = UtcDateTimes.ToStored(batch.CreatedAt),
        Items = batch.Items.Select(ShiftBatchItemDocument.FromDomain).ToList()
    };

    public ShiftBatch ToDomain() => new(BatchId, Actor, UtcDateTimes.FromStored(CreatedAt),
        Items.Select(item => item.ToDomain()).ToList());
}
