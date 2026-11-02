using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

[BsonIgnoreExtraElements]
internal sealed class RolePermissionDocument
{
    [BsonId]
    public string RoleCode { get; init; } = string.Empty;

    [BsonElement("permissions")]
    public string[] Permissions { get; init; } = [];
}
