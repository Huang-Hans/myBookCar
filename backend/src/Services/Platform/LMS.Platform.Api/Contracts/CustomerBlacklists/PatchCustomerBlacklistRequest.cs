using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.CustomerBlacklists;

/// <summary>
/// 局部更新客户限制（黑名单）请求体；至少传一项，未出现的键保持原值。
/// </summary>
/// <param name="CustomerName">客户姓名；未传保持原值，不接受 null 或空白。</param>
/// <param name="MemberId">会员 ID；未传保持原值，不接受 null 或空白，仍做不区分大小写重复校验。</param>
/// <param name="Reason">限制原因；未传保持原值，不接受 null 或空白。</param>
/// <param name="EffectiveDate">生效日期（YYYY-MM-DD，含当天）；未传保持原值，不接受 null。</param>
/// <param name="ExpiryDate">失效日期（YYYY-MM-DD，含当天，不得早于 EffectiveDate）；未传保持原值，不接受 null。</param>
/// <param name="IsActive">是否启用；未传保持原值，不接受 null。</param>
public sealed record PatchCustomerBlacklistRequest(
    Optional<string?> CustomerName,
    Optional<string?> MemberId,
    Optional<string?> Reason,
    Optional<string?> EffectiveDate,
    Optional<string?> ExpiryDate,
    Optional<bool?> IsActive);
