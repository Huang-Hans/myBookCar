using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class TripModificationDocument
{
    [BsonElement("reason")]
    public string? Reason { get; init; }

    [BsonElement("comments")]
    public string? Comments { get; init; }

    [BsonElement("modifiedBy")]
    public string ModifiedBy { get; init; } = string.Empty;

    [BsonElement("modifiedByName")]
    public string? ModifiedByName { get; init; }

    [BsonElement("modifiedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ModifiedAt { get; init; }

    [BsonElement("changedFields")]
    public List<string> ChangedFields { get; init; } = [];

    public static TripModificationDocument FromDomain(TripModification modification) => new()
    {
        Reason = modification.Reason is null ? null : WireEnum.ToWire(modification.Reason.Value),
        Comments = modification.Comments,
        ModifiedBy = modification.ModifiedBy,
        ModifiedByName = modification.ModifiedByName,
        ModifiedAt = UtcDateTimes.ToStored(modification.ModifiedAt),
        ChangedFields = modification.ChangedFields.ToList()
    };

    public TripModification ToDomain() => new(
        string.IsNullOrEmpty(Reason) ? null : StoredValues.ParseEnum<TripModificationReason>(Reason, "modifications.reason"),
        Comments,
        ModifiedBy,
        ModifiedByName,
        UtcDateTimes.FromStored(ModifiedAt),
        ChangedFields);
}
