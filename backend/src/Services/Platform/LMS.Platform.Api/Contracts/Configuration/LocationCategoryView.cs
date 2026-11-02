namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 地点分类视图，提供上传配置中的分类代码、名称与启用状态。
/// </summary>
/// <param name="CategoryCode">分类代码，使用上传配置的稳定代码而非显示名称。</param>
/// <param name="Name">分类显示名称。</param>
/// <param name="Description">分类描述。</param>
/// <param name="IsActive">是否启用（Active/Inactive）。</param>
public sealed record LocationCategoryView(string CategoryCode, string Name, string Description, bool IsActive);
