using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Users;

// 合同 CreatePortalUserRequest。所有字段都按可空读取，缺失由校验统一报告为字段错误。
// PropertyId 必须出现在请求中：全物业授权时显式传 null。
/// <summary>
/// 创建门户用户请求体；字段缺失由校验统一报告为字段错误。
/// </summary>
/// <param name="Name">用户姓名，必填且非空白。</param>
/// <param name="CorporateEmail">企业邮箱，必填且非空白；不区分大小写去重并保留规范化值。</param>
/// <param name="RoleCode">角色代码，必填，SUPER_ADMIN / OPERATIONS_MANAGER / DISPATCHER / FINANCE / PROPERTY_CS_STAFF。</param>
/// <param name="HasAllPropertyAccess">是否授予全部物业访问，必填；必须显式给出，不能由空物业数组推断。</param>
/// <param name="PropertyId">物业代码键，必填；全物业授权时显式传 null，否则为单一物业代码且不得为空白。</param>
/// <param name="IsActive">用户启用状态，必填。</param>
public sealed record CreatePortalUserRequest(
    string? Name,
    string? CorporateEmail,
    string? RoleCode,
    bool? HasAllPropertyAccess,
    Optional<string?> PropertyId,
    bool? IsActive);
