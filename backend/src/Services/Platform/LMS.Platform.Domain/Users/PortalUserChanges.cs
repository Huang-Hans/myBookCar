using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Domain.Users;

// 应用用户的局部更新。PropertyId 允许显式 null（全物业授权时清空单一物业）；其余字段不接受 null。
public sealed record PortalUserChanges(
    Optional<string> Name,
    Optional<string> CorporateEmail,
    Optional<PortalRole> Role,
    Optional<bool> HasAllPropertyAccess,
    Optional<string> PropertyId,
    Optional<bool> IsActive)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(
            ("name", Name.IsSet),
            ("corporateEmail", CorporateEmail.IsSet),
            ("roleCode", Role.IsSet),
            ("hasAllPropertyAccess", HasAllPropertyAccess.IsSet),
            ("propertyId", PropertyId.IsSet),
            ("isActive", IsActive.IsSet));
}
