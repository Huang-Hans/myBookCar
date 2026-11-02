using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Application.Preferences.Ports;

public interface IDriverPreferencesRepository
{
    Task<DriverPreferences?> FindAsync(string driverId, CancellationToken cancellationToken);

    Task<DriverPreferences?> FindAsync(string driverId, ITransactionContext context, CancellationToken cancellationToken);

    // 同一司机已有偏好记录（并发首次写入）时抛出 DuplicateRecordException。
    Task InsertAsync(DriverPreferences preferences, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。
    Task<bool> TryReplaceAsync(DriverPreferences updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
