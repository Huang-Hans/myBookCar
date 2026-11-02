namespace LMS.Platform.Api.Contracts.CustomerBlacklists;

/// <summary>
/// 客户限制（黑名单）视图，映射 customer_blacklists；空值字段省略。
/// </summary>
/// <param name="BlacklistId">限制记录标识。</param>
/// <param name="CustomerName">客户姓名。</param>
/// <param name="MemberId">会员 ID，不区分大小写参与重复校验。</param>
/// <param name="Reason">限制原因。</param>
/// <param name="EffectiveDate">生效日期，YYYY-MM-DD 且含当天。</param>
/// <param name="ExpiryDate">失效日期，YYYY-MM-DD 且含当天。</param>
/// <param name="IsActive">是否启用；停用时仍保留记录。</param>
/// <param name="Version">乐观锁版本号，配合 X-Expected-Version 头做并发控制。</param>
public sealed record CustomerBlacklistView(
    string BlacklistId,
    string CustomerName,
    string MemberId,
    string Reason,
    string EffectiveDate,
    string ExpiryDate,
    bool IsActive,
    long Version);
