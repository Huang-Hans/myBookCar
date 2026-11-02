using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Application.Authentication.Services;
using LMS.Platform.Domain.Authentication;

namespace LMS.Platform.Application.Authentication.Handlers;

// 刷新：凭据必须属于当前入口的身份类型且未消费、未撤销、未过期；所属服务确认记录仍可登录后原子轮换。
// 已消费的凭据再次出现视为重用，撤销整条刷新链。
internal sealed class RefreshAppSessionHandler(
    IAppRefreshTokenRepository tokens,
    IAppCredentialVerifier credentials,
    IAppAccessTokenIssuer issuer,
    AppSessionStore sessions,
    TimeProvider timeProvider) : IRefreshAppSessionHandler
{
    public async Task<AppAuthResult> HandleAsync(AppRefreshCommand command, CancellationToken cancellationToken)
    {
        if (!issuer.IsAvailable)
        {
            return AppAuthResult.Failed(AppAuthOutcome.SourceUnavailable);
        }

        var current = await tokens.FindByHashAsync(RefreshTokenSecret.HashOf(command.RefreshToken), cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (current is null || current.PrincipalType != command.Type || current.IsExpiredAt(now))
        {
            return AppAuthResult.Failed(AppAuthOutcome.NotAuthenticated);
        }

        return current.IsUsableAt(now)
            ? await RotateAsync(current, command, cancellationToken)
            : await RejectUnusableAsync(current, command, cancellationToken);
    }

    private async Task<AppAuthResult> RotateAsync(AppRefreshToken current, AppRefreshCommand command, CancellationToken cancellationToken)
    {
        var eligibility = await credentials.CheckEligibilityAsync(current.PrincipalType, current.EntityId, cancellationToken);
        if (eligibility.Outcome != CredentialCheckOutcome.Verified)
        {
            return AppAuthResult.Failed(eligibility.Outcome == CredentialCheckOutcome.Unavailable
                ? AppAuthOutcome.SourceUnavailable
                : AppAuthOutcome.NotAuthenticated);
        }

        var session = await sessions.RotateAsync(current, command.TraceId, cancellationToken);

        return session is null ? AppAuthResult.Failed(AppAuthOutcome.NotAuthenticated) : AppAuthResult.Succeeded(session);
    }

    // 已消费 = 重用；已被退出撤销的凭据只拒绝，不再重复撤销。
    private async Task<AppAuthResult> RejectUnusableAsync(AppRefreshToken current, AppRefreshCommand command, CancellationToken cancellationToken)
    {
        if (current.ConsumedAt is not null && current.RevokedAt is null)
        {
            await sessions.RevokeFamilyAsync(current, AppSessionAuditFactory.ReuseRevokedOperation, command.TraceId, cancellationToken);
        }

        return AppAuthResult.Failed(AppAuthOutcome.NotAuthenticated);
    }
}
