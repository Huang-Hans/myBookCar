namespace LMS.Platform.Application.Configuration.Models;

// 只读粗粒度权限矩阵，仅作展示参考，不参与实际授权判断（Q01）。
public sealed record RolePermissionReference(
    string RoleCode,
    string RoleName,
    bool CanManageDrivers,
    bool CanViewReports,
    bool CanReviewApprovals,
    bool CanManageConfiguration);
