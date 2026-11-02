using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Authorization.Options;
using LMS.Platform.Application.Authorization.Ports;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Authorization.Services;

internal sealed class RolePermissionPolicy(
    IOptions<RolePermissionOptions> options,
    IRolePermissionRepository roles)
{
    public async Task<IReadOnlyCollection<string>> PermissionsForAsync(PortalRole role, CancellationToken cancellationToken)
    {
        if (role == PortalRole.SuperAdmin)
        {
            return PermissionCodes.All;
        }

        var roleCode = WireEnum.ToWire(role);
        var permissions = await roles.FindPermissionsAsync(roleCode, cancellationToken)
            ?? (options.Value.RolePermissions.TryGetValue(roleCode, out var configured) ? configured : []);

        return permissions.Where(permission => PermissionCodes.All.Contains(permission, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal).ToArray();
    }
}
