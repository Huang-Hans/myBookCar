namespace LMS.Platform.Domain.CustomerBlacklists;

// 客户限制主档。一个会员当前只有一条限制；停用保留记录。
// EffectiveDate、ExpiryDate 是不含时刻的业务日期（含当天），不能当作 UTC 午夜。
public sealed record CustomerBlacklist(
    string BlacklistId,
    string CustomerName,
    string MemberId,
    string Reason,
    DateOnly EffectiveDate,
    DateOnly ExpiryDate,
    bool IsActive,
    long Version)
{
    private const string IdPrefix = "blacklist_";
    private const long FirstVersion = 1;

    public static CustomerBlacklist Create(
        string customerName,
        string memberId,
        string reason,
        DateOnly effectiveDate,
        DateOnly expiryDate,
        bool isActive) =>
        new(
            IdPrefix + Guid.CreateVersion7().ToString("N"),
            customerName, memberId, reason, effectiveDate, expiryDate, isActive, FirstVersion);

    public CustomerBlacklist Apply(CustomerBlacklistChanges changes) =>
        this with
        {
            CustomerName = changes.CustomerName.ValueOr(CustomerName),
            MemberId = changes.MemberId.ValueOr(MemberId),
            Reason = changes.Reason.ValueOr(Reason),
            EffectiveDate = changes.EffectiveDate.ValueOr(EffectiveDate),
            ExpiryDate = changes.ExpiryDate.ValueOr(ExpiryDate),
            IsActive = changes.IsActive.ValueOr(IsActive)
        };
}
