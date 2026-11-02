using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Authorization.Models;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Authorization.Services;

// 由应用用户得到调用者授权。用户不存在、已停用或物业范围不一致时返回 null。
internal sealed class CallerAccessFactory(RolePermissionPolicy policy)
{
    public async Task<CallerAccess?> CreateAsync(PortalUser? user, CancellationToken cancellationToken)
    {
        if (user is null || !user.IsActive || !HasConsistentScope(user))
        {
            return null;
        }

        var permissions = await policy.PermissionsForAsync(user.Role, cancellationToken);
        return new CallerAccess(
            user.UserId, WireEnum.ToWire(user.Role), user.HasAllPropertyAccess, user.PropertyId, permissions, user.Name);
    }

    // 非全物业用户必须有单一物业；数据不一致时按无授权处理，而不是猜测范围。
    private static bool HasConsistentScope(PortalUser user) =>
        user.HasAllPropertyAccess ? user.PropertyId is null : !string.IsNullOrWhiteSpace(user.PropertyId);
}
