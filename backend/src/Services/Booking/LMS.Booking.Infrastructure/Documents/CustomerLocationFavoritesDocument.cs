using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Customers;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

// 客户某一用途分组的收藏：_id 为 "{guestId}:{usage}"，因此同一客户同一用途至多一条；items 顺序即保存顺序。
internal sealed class CustomerLocationFavoritesDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    [BsonElement("guestId")]
    public string GuestId { get; init; } = string.Empty;

    [BsonElement("usage")]
    public string Usage { get; init; } = string.Empty;

    [BsonElement("items")]
    public List<LocationFavoriteDocument> Items { get; init; } = [];

    [BsonElement("updatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? UpdatedAt { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static string IdOf(string guestId, FavoriteUsage usage) => $"{guestId}:{WireEnum.ToWire(usage)}";

    public static CustomerLocationFavoritesDocument FromDomain(CustomerLocationFavorites favorites) => new()
    {
        Id = IdOf(favorites.GuestId, favorites.Usage),
        GuestId = favorites.GuestId,
        Usage = WireEnum.ToWire(favorites.Usage),
        Items = favorites.Items.Select(LocationFavoriteDocument.FromDomain).ToList(),
        UpdatedAt = favorites.UpdatedAt is null ? null : UtcDateTimes.ToStored(favorites.UpdatedAt.Value),
        Version = favorites.Version
    };

    public CustomerLocationFavorites ToDomain() => new(
        GuestId,
        StoredValues.ParseEnum<FavoriteUsage>(Usage, "usage"),
        Items.Select(item => item.ToDomain()).ToList(),
        UpdatedAt is null ? null : UtcDateTimes.FromStored(UpdatedAt.Value),
        Version);
}
