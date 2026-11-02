using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Definitions;

// 权限矩阵和集成卡片不分页，也没有排序参数；定义只提供稳定代码，列表保持来源中的顺序。
public static class RolePermissionReferenceDefinition
{
    public static ConfigurationListDefinition<RolePermissionReference> Create() =>
        new(
            item => item.RoleCode,
            new Dictionary<string, Func<RolePermissionReference, IComparable?>> { ["roleCode"] = item => item.RoleCode },
            item => [item.RoleCode, item.RoleName],
            "roleCode",
            "roleCode");
}
