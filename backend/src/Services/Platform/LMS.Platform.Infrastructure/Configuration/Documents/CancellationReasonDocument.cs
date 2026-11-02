using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class CancellationReasonDocument
{
    [BsonElement("reasonCode")]
    public string ReasonCode { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("pointsReturnPolicyCode")]
    public string PointsReturnPolicyCode { get; init; } = string.Empty;

    [BsonElement("pointsReturnReference")]
    public string PointsReturnReference { get; init; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; init; } = string.Empty;

    public CancellationReason ToItem() => new(ReasonCode, Name, PointsReturnPolicyCode, PointsReturnReference, Source);
}
