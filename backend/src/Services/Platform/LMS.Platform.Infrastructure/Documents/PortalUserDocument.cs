using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Platform.Domain.Users;
using LMS.Platform.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Documents;

internal sealed class PortalUserDocument
{
    [BsonId]
    public string UserId { get; init; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; init; } = string.Empty;

    [BsonElement("corporateEmail")]
    public string CorporateEmail { get; init; } = string.Empty;

    [BsonElement("roleCode")]
    public string RoleCode { get; init; } = string.Empty;

    [BsonElement("hasAllPropertyAccess")]
    public bool HasAllPropertyAccess { get; init; }

    [BsonElement("propertyId")]
    public string? PropertyId { get; init; }

    [BsonElement("isActive")]
    public bool IsActive { get; init; }

    [BsonElement("identityProvider")]
    public string IdentityProvider { get; init; } = string.Empty;

    [BsonElement("subjectId")]
    public string? SubjectId { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static PortalUserDocument FromDomain(PortalUser user) => new()
    {
        UserId = user.UserId,
        Name = user.Name,
        CorporateEmail = user.CorporateEmail,
        RoleCode = WireEnum.ToWire(user.Role),
        HasAllPropertyAccess = user.HasAllPropertyAccess,
        PropertyId = user.PropertyId,
        IsActive = user.IsActive,
        IdentityProvider = user.IdentityProvider,
        SubjectId = user.SubjectId,
        Version = user.Version
    };

    public PortalUser ToDomain() => new(
        UserId,
        Name,
        CorporateEmail,
        StoredValues.ParseEnum<PortalRole>(RoleCode, "roleCode"),
        HasAllPropertyAccess,
        PropertyId,
        IsActive,
        IdentityProvider,
        SubjectId,
        Version);
}
