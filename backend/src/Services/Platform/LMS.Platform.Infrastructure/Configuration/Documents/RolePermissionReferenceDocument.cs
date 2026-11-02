using LMS.Platform.Application.Configuration.Models;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Platform.Infrastructure.Configuration.Documents;

internal sealed class RolePermissionReferenceDocument
{
    [BsonElement("roleCode")]
    public string RoleCode { get; init; } = string.Empty;

    [BsonElement("roleName")]
    public string RoleName { get; init; } = string.Empty;

    [BsonElement("canManageDrivers")]
    public bool CanManageDrivers { get; init; }

    [BsonElement("canViewReports")]
    public bool CanViewReports { get; init; }

    [BsonElement("canReviewApprovals")]
    public bool CanReviewApprovals { get; init; }

    [BsonElement("canManageConfiguration")]
    public bool CanManageConfiguration { get; init; }

    public RolePermissionReference ToItem() =>
        new(RoleCode, RoleName, CanManageDrivers, CanViewReports, CanReviewApprovals, CanManageConfiguration);
}
