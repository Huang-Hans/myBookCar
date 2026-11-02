namespace LMS.Platform.Application.Quotas;

public interface IPropertyQuotaDirectory
{
    // Booking 的权威每日限额（按调用者可见物业）。Booking 不可用时返回 null，调用方按合同省略 dailyQuota，不使用上传配置里的旧值。
    Task<IReadOnlyDictionary<string, int>?> GetDailyQuotasAsync(CancellationToken cancellationToken);
}
