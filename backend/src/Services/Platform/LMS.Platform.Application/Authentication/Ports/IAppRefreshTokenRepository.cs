using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Authentication;

namespace LMS.Platform.Application.Authentication.Ports;

public interface IAppRefreshTokenRepository
{
    Task<AppRefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task InsertAsync(AppRefreshToken token, ITransactionContext context, CancellationToken cancellationToken);

    // 仅当存储的版本等于期望版本时才整体替换；版本已变化（并发消费）返回 false。
    Task<bool> TryReplaceAsync(AppRefreshToken updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);

    // 撤销整条轮换链上尚未撤销的凭据；已撤销的保持原撤销时间。
    Task RevokeFamilyAsync(string familyId, DateTimeOffset revokedAt, ITransactionContext context, CancellationToken cancellationToken);
}
