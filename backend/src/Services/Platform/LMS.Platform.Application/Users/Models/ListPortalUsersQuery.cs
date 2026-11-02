using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Platform.Application.Users.Models;

// PropertyId 为显式的物业筛选；省略时只在 Scope 授权范围内查询，不能因此获得全物业权限。
public sealed record ListPortalUsersQuery(ListQuery List, PropertyScope Scope, string? PropertyId, bool? IsActive);
