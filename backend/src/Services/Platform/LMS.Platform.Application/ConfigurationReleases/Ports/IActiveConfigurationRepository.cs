using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Configuration;

namespace LMS.Platform.Application.ConfigurationReleases.Ports;

public interface IActiveConfigurationRepository
{
    Task<ActiveConfiguration?> FindAsync(string environment, ITransactionContext context, CancellationToken cancellationToken);

    // 事务之外的读取，用于配置正文读取时确定当前生效的发布。
    Task<ActiveConfiguration?> FindAsync(string environment, CancellationToken cancellationToken);

    // current 为 null 时创建首个生效记录，重复创建抛出 DuplicateRecordException；
    // 否则按 current.Version 做条件更新。返回 false 表示版本已被他人推进。
    Task<bool> AdvanceAsync(
        ActiveConfiguration? current,
        ConfigurationRelease release,
        ITransactionContext context,
        CancellationToken cancellationToken);
}
