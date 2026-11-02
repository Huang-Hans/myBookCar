using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Users;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Api.Mapping.Users;

internal static class CurrentUserViewMapper
{
    public static CurrentUserView ToView(PortalUser user, CallerAuthorization caller) => new(
        user.UserId,
        user.Name,
        user.CorporateEmail,
        WireEnum.ToWire(user.Role),
        caller.HasAllPropertyAccess,
        caller.PropertyId,
        caller.Permissions.Order(StringComparer.Ordinal).ToList());
}
