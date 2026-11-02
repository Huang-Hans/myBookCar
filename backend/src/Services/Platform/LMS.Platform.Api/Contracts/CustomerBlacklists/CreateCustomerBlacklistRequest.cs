namespace LMS.Platform.Api.Contracts.CustomerBlacklists;

/// <summary>
/// 创建客户限制（黑名单）请求体；仅包含原型表单可编辑字段，未知字段拒绝。
/// </summary>
/// <param name="CustomerName">客户姓名，必填且非空白。</param>
/// <param name="MemberId">会员 ID，必填；不区分大小写做重复校验。</param>
/// <param name="Reason">限制原因，必填。</param>
/// <param name="EffectiveDate">生效日期，YYYY-MM-DD 且含当天。</param>
/// <param name="ExpiryDate">失效日期，YYYY-MM-DD 且含当天，不得早于 EffectiveDate。</param>
/// <param name="IsActive">是否启用；停用时保留记录。</param>
public sealed record CreateCustomerBlacklistRequest(
    string? CustomerName,
    string? MemberId,
    string? Reason,
    string? EffectiveDate,
    string? ExpiryDate,
    bool? IsActive);
