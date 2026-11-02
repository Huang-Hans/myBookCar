namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 角色权限矩阵视图；只读粗粒度矩阵，不能代替实际端点权限与数据范围校验。
/// </summary>
/// <param name="RoleCode">五角色之一的角色代码。</param>
/// <param name="RoleName">角色显示名称。</param>
/// <param name="CanManageDrivers">是否可管理司机（Drivers 列）。</param>
/// <param name="CanViewReports">是否可查看报表（Reports 列）。</param>
/// <param name="CanReviewApprovals">是否可审核审批（Approvals 列）。</param>
/// <param name="CanManageConfiguration">是否可管理配置（Configuration 列）。</param>
public sealed record RolePermissionView(
    string RoleCode,
    string RoleName,
    bool CanManageDrivers,
    bool CanViewReports,
    bool CanReviewApprovals,
    bool CanManageConfiguration);
