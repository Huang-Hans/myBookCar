using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class TravelBufferDocument
{
    [BsonElement("routeCategoryCode")]
    public string RouteCategoryCode { get; init; } = string.Empty;

    [BsonElement("routeCategory")]
    public string RouteCategory { get; init; } = string.Empty;

    [BsonElement("bufferMinutes")]
    public int BufferMinutes { get; init; }

    [BsonElement("navigationProvider")]
    public string NavigationProvider { get; init; } = string.Empty;

    [BsonElement("isTrafficAwareEtaEnabled")]
    public bool IsTrafficAwareEtaEnabled { get; init; }

    [BsonElement("applicationRule")]
    public string ApplicationRule { get; init; } = string.Empty;

    public TravelBuffer ToItem() => new(
        RouteCategoryCode,
        RouteCategory,
        BufferMinutes,
        StoredValues.ParseEnum<NavigationProvider>(NavigationProvider, "navigationProvider"),
        IsTrafficAwareEtaEnabled,
        ApplicationRule);
}
