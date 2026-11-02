using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Users;

/// <summary>
/// 局部更新门户用户请求体；至少传一项，未出现的键保持原值。
/// </summary>
/// <param name="Name">用户姓名；未传保持原值，不接受 null 或空白。</param>
/// <param name="CorporateEmail">企业邮箱；未传保持原值，不接受 null 或空白，仍做不区分大小写去重。</param>
/// <param name="RoleCode">角色代码；未传保持原值，取值须为五角色字面值之一。</param>
/// <param name="HasAllPropertyAccess">是否授予全部物业访问；未传保持原值，不接受 null。</param>
/// <param name="PropertyId">物业代码；未传保持原值，键出现且为 null 表示改为全物业授权，否则须为非空白物业代码。</param>
/// <param name="IsActive">用户启用状态；未传保持原值，不接受 null。</param>
public sealed record PatchPortalUserRequest(
    Optional<string?> Name,
    Optional<string?> CorporateEmail,
    Optional<string?> RoleCode,
    Optional<bool?> HasAllPropertyAccess,
    Optional<string?> PropertyId,
    Optional<bool?> IsActive);
