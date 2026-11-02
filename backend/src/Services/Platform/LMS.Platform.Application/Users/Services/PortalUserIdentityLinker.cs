using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Authorization.Models;
using LMS.Platform.Application.Users.Options;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Domain.Auditing;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Users.Services;

// 员工首次以企业身份登录时，把已验证的令牌主体一次性关联到同企业邮箱、尚未关联且启用的应用用户。
// 未开启邮箱关联、条件不满足、并发中被他人先关联或主体已被占用时一律返回 null，按无授权处理。
internal sealed class PortalUserIdentityLinker(
    IPortalUserRepository users,
    ITransactionRunner transactionRunner,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider,
    IOptions<PortalUserOptions> options)
{
    private const string Operation = "portal-user.identity-link";
    private const string ResourceType = "portal-user";
    private const string ChangeSummary = "identity subject linked";

    public async Task<PortalUser?> TryLinkAsync(CallerLookup lookup, CancellationToken cancellationToken)
    {
        if (!options.Value.LinkByVerifiedEmail || string.IsNullOrWhiteSpace(lookup.VerifiedEmail))
        {
            return null;
        }

        var candidate = await users.FindUnlinkedByEmailAsync(
            lookup.IdentityProvider, PortalUser.NormalizeEmail(lookup.VerifiedEmail), cancellationToken);

        return candidate is { IsActive: true } ? await LinkAsync(candidate, lookup, cancellationToken) : null;
    }

    private async Task<PortalUser?> LinkAsync(PortalUser candidate, CallerLookup lookup, CancellationToken cancellationToken)
    {
        var linked = candidate.LinkSubject(lookup.SubjectId);
        try
        {
            var replaced = await transactionRunner.ExecuteAsync(
                (context, token) => ReplaceWithAuditAsync(candidate, linked, lookup.TraceId, context, token),
                cancellationToken);

            return replaced
                ? linked with { Version = candidate.Version + 1 }
                : await users.FindBySubjectAsync(lookup.IdentityProvider, lookup.SubjectId, cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            return null;
        }
    }

    private async Task<bool> ReplaceWithAuditAsync(
        PortalUser original,
        PortalUser linked,
        string traceId,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        if (!await users.TryReplaceAsync(linked, original.Version, context, cancellationToken))
        {
            return false;
        }

        await auditRecords.InsertAsync(BuildAudit(linked, traceId), context, cancellationToken);

        return true;
    }

    private AuditRecord BuildAudit(PortalUser user, string traceId) =>
        AuditRecordFactory.Succeeded(
            new CommandContext(user.UserId, traceId, ScopeOf(user)),
            Operation,
            ResourceType,
            user.UserId,
            timeProvider.GetUtcNow(),
            ChangeSummary,
            user.PropertyId);

    private static PropertyScope ScopeOf(PortalUser user) =>
        user.HasAllPropertyAccess || user.PropertyId is null ? PropertyScope.AllProperties : PropertyScope.ForProperty(user.PropertyId);
}
