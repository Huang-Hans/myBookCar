using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Authorization.Options;

internal sealed class RolePermissionOptionsValidator : IValidateOptions<RolePermissionOptions>
{
    public ValidateOptionsResult Validate(string? name, RolePermissionOptions options)
    {
        var failures = new List<string>();
        foreach (var (role, permissions) in options.RolePermissions)
        {
            ValidateRole(role, failures);
            failures.AddRange(permissions.Where(permission => !PermissionCodes.All.Contains(permission))
                .Select(permission => $"{RolePermissionOptions.SectionName}:RolePermissions:{role} contains unknown permission '{permission}'."));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRole(string role, List<string> failures)
    {
        if (!WireEnum.TryParse<PortalRole>(role, out var parsed) || parsed == PortalRole.SuperAdmin)
        {
            failures.Add($"{RolePermissionOptions.SectionName}:RolePermissions has invalid or fixed role '{role}'.");
        }
    }
}
