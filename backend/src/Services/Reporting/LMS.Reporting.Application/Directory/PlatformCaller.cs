namespace LMS.Reporting.Application.Directory;

// Platform 对调用者授权的解析结果：应用用户、角色、物业范围和生效权限。
public sealed record PlatformCaller(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    IReadOnlyCollection<string> Permissions);
