using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Staff;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class StaffPreferencesDocument
{
    [BsonId]
    public string UserId { get; init; } = string.Empty;

    [BsonElement("station")]
    public string Station { get; init; } = string.Empty;

    [BsonElement("defaultNotificationChannel")]
    public string DefaultNotificationChannel { get; init; } = string.Empty;

    [BsonElement("confirmationLanguage")]
    public string ConfirmationLanguage { get; init; } = string.Empty;

    [BsonElement("enabledNotificationTypes")]
    public List<string> EnabledNotificationTypes { get; init; } = [];

    [BsonElement("favoriteLocationIds")]
    public List<string> FavoriteLocationIds { get; init; } = [];

    [BsonElement("recentLocationIds")]
    public List<string> RecentLocationIds { get; init; } = [];

    public static StaffPreferencesDocument FromDomain(StaffPreferences preferences) => new()
    {
        UserId = preferences.UserId,
        Station = preferences.Station,
        DefaultNotificationChannel = WireEnum.ToWire(preferences.DefaultNotificationChannel),
        ConfirmationLanguage = preferences.ConfirmationLanguage,
        EnabledNotificationTypes = preferences.EnabledNotificationTypes.ToList(),
        FavoriteLocationIds = preferences.FavoriteLocationIds.ToList(),
        RecentLocationIds = preferences.RecentLocationIds.ToList()
    };

    public StaffPreferences ToDomain() => new(
        UserId,
        Station,
        StoredValues.ParseEnum<StaffNotificationChannel>(DefaultNotificationChannel, "defaultNotificationChannel"),
        ConfirmationLanguage,
        EnabledNotificationTypes,
        FavoriteLocationIds,
        RecentLocationIds);
}
