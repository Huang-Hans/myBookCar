using LMS.BuildingBlocks.Core.Authorization;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Options;
using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Domain.Authentication;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Authentication.Services;

// 刷新凭据的持久化与会话组装：开始新会话、原子轮换、撤销整条刷新链。每次变更与其审计记录在同一本地事务内提交。
internal sealed class AppSessionStore(
    ITransactionRunner transactions,
    IAppRefreshTokenRepository tokens,
    IAuditRecordRepository audits,
    IAppAccessTokenIssuer issuer,
    IOptions<AppAuthenticationOptions> options,
    TimeProvider timeProvider)
{
    public async Task<AppSession> StartAsync(PrincipalType type, string entityId, string traceId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var secret = RefreshTokenSecret.Create();
        var token = AppRefreshToken.Issue(
            secret.Hash, type, entityId, AppRefreshToken.NewFamilyId(), now, now + options.Value.RefreshTokenLifetime);
        await transactions.ExecuteAsync(async (context, ct) =>
        {
            await tokens.InsertAsync(token, context, ct);
            await audits.InsertAsync(
                AppSessionAuditFactory.Succeeded(AppSessionAuditFactory.LoginOperation, type, entityId, token.FamilyId, traceId, now), context, ct);
            return true;
        }, cancellationToken);

        return Assemble(token, secret.Plain, now);
    }

    // 条件消费旧凭据并插入同一刷新链的后继（绝对到期时间不变）。旧凭据已被并发消费时撤销整条链并返回 null。
    public async Task<AppSession?> RotateAsync(AppRefreshToken current, string traceId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var secret = RefreshTokenSecret.Create();
        var next = AppRefreshToken.Issue(secret.Hash, current.PrincipalType, current.EntityId, current.FamilyId, now, current.ExpiresAt);
        var rotated = await transactions.ExecuteAsync(
            (context, ct) => TryRotateAsync(current, next, traceId, now, context, ct), cancellationToken);

        return rotated ? Assemble(next, secret.Plain, now) : null;
    }

    public Task RevokeFamilyAsync(AppRefreshToken token, string operation, string traceId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return transactions.ExecuteAsync(async (context, ct) =>
        {
            await tokens.RevokeFamilyAsync(token.FamilyId, now, context, ct);
            await audits.InsertAsync(
                AppSessionAuditFactory.Succeeded(operation, token.PrincipalType, token.EntityId, token.FamilyId, traceId, now), context, ct);
            return true;
        }, cancellationToken);
    }

    public Task RecordLoginFailureAsync(PrincipalType type, string traceId, CancellationToken cancellationToken)
    {
        var record = AppSessionAuditFactory.LoginFailed(type, traceId, timeProvider.GetUtcNow());

        return transactions.ExecuteAsync(async (context, ct) =>
        {
            await audits.InsertAsync(record, context, ct);
            return true;
        }, cancellationToken);
    }

    private async Task<bool> TryRotateAsync(
        AppRefreshToken current,
        AppRefreshToken next,
        string traceId,
        DateTimeOffset now,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        if (!await tokens.TryReplaceAsync(current.Consume(next.TokenId, now), current.Version, context, cancellationToken))
        {
            await tokens.RevokeFamilyAsync(current.FamilyId, now, context, cancellationToken);
            await audits.InsertAsync(Audit(AppSessionAuditFactory.ReuseRevokedOperation, current, traceId, now), context, cancellationToken);

            return false;
        }

        await tokens.InsertAsync(next, context, cancellationToken);
        await audits.InsertAsync(Audit(AppSessionAuditFactory.RefreshOperation, current, traceId, now), context, cancellationToken);

        return true;
    }

    private static Domain.Auditing.AuditRecord Audit(string operation, AppRefreshToken token, string traceId, DateTimeOffset now) =>
        AppSessionAuditFactory.Succeeded(operation, token.PrincipalType, token.EntityId, token.FamilyId, traceId, now);

    private AppSession Assemble(AppRefreshToken token, string plainRefreshToken, DateTimeOffset now)
    {
        var lifetime = options.Value.AccessTokenLifetime;
        var accessToken = issuer.Issue(new AppAccessTokenRequest(token.PrincipalType, token.EntityId, token.FamilyId, now, lifetime));

        return new AppSession(
            accessToken,
            (int)lifetime.TotalSeconds,
            plainRefreshToken,
            (int)(token.ExpiresAt - now).TotalSeconds,
            token.PrincipalType);
    }
}
