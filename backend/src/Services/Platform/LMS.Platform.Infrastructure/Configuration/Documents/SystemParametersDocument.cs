using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class SystemParametersDocument
{
    [BsonElement("language")]
    public string Language { get; init; } = string.Empty;

    [BsonElement("timeZone")]
    public string TimeZone { get; init; } = string.Empty;

    [BsonElement("dateFormat")]
    public string DateFormat { get; init; } = string.Empty;

    [BsonElement("sessionTimeoutMinutes")]
    public int SessionTimeoutMinutes { get; init; }

    [BsonElement("passwordPolicyReference")]
    public string PasswordPolicyReference { get; init; } = string.Empty;

    [BsonElement("pointsRedemptionMode")]
    public string PointsRedemptionMode { get; init; } = string.Empty;

    [BsonElement("pointsExpiryReference")]
    public string PointsExpiryReference { get; init; } = string.Empty;

    public SystemParameters ToModel() => new(
        Language, TimeZone, DateFormat, SessionTimeoutMinutes, PasswordPolicyReference, PointsRedemptionMode, PointsExpiryReference);
}
