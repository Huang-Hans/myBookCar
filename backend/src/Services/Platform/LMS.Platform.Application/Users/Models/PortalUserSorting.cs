namespace LMS.Platform.Application.Users.Models;

// 合同 PortalUserQuery 的排序约定；接入层校验、持久化层映射都以此为准。
public static class PortalUserSorting
{
    public static IReadOnlyList<string> Fields { get; } =
        ["name", "corporateEmail", "roleCode", "propertyId", "identityProvider", "isActive", "userId"];

    public const string DefaultSort = "name,userId";
    public const string StableField = "userId";
}
