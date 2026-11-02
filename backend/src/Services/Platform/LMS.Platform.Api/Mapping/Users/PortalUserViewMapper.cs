using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.Users;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Api.Mapping.Users;

internal static class PortalUserViewMapper
{
    public static PortalUserView ToView(PortalUser user) => new(
        user.UserId,
        user.Name,
        user.CorporateEmail,
        WireEnum.ToWire(user.Role),
        user.HasAllPropertyAccess,
        user.PropertyId,
        user.IsActive,
        user.IdentityProvider,
        user.Version);
}
