namespace LMS.Platform.Api.Contracts.Internal;

// 服务间使用：已验证调用者在 Platform 的授权。不经网关发布，不属于对外的 76 个管理接口。
/// <summary>
/// 服务间调用者授权视图：返回当前令牌所属调用者在 Platform 的用户、角色、物业范围与权限。
/// </summary>
/// <param name="UserId">调用者用户标识。</param>
/// <param name="RoleCode">调用者角色代码。</param>
/// <param name="HasAllPropertyAccess">是否拥有全部物业访问授权。</param>
/// <param name="PropertyId">单一物业代码；全物业授权时为 null。</param>
/// <param name="Permissions">调用者拥有的权限代码列表。</param>
/// <param name="Name">调用者姓名，供下游服务保存经办人快照；缺失时省略。</param>
public sealed record CallerAuthorizationView(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    IReadOnlyList<string> Permissions,
    string? Name = null);
