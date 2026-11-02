using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Domain.Auditing;

namespace LMS.Platform.Application.Authentication.Services;

// 登录/刷新/退出的审计事实。只记录主体类型、业务实体标识和刷新链标识，不记录登录标识、密码或任何令牌。
internal static class AppSessionAuditFactory
{
    public const string LoginOperation = "app-session.login";
    public const string LoginFailedOperation = "app-session.login-failed";
    public const string RefreshOperation = "app-session.refresh";
    public const string LogoutOperation = "app-session.logout";
    public const string ReuseRevokedOperation = "app-session.reuse-revoked";

    private const string ResourceType = "app-session";
    private const string ServiceName = "platform";
    private const string AnonymousActor = "anonymous";

    public static AuditRecord Succeeded(
        string operation,
        PrincipalType type,
        string entityId,
        string familyId,
        string traceId,
        DateTimeOffset occurredAt) =>
        Create(operation, entityId, familyId, type, traceId, occurredAt, AuditOutcome.Succeeded);

    public static AuditRecord LoginFailed(PrincipalType type, string traceId, DateTimeOffset occurredAt) =>
        Create(LoginFailedOperation, AnonymousActor, "n/a", type, traceId, occurredAt, AuditOutcome.Failed);

    private static AuditRecord Create(
        string operation,
        string actor,
        string familyId,
        PrincipalType type,
        string traceId,
        DateTimeOffset occurredAt,
        AuditOutcome outcome) =>
        new(
            AuditRecord.NewId(),
            actor,
            operation,
            ResourceType,
            familyId,
            occurredAt,
            outcome,
            traceId,
            ServiceName,
            $"principalType={WireEnum.ToWire(type)}",
            null);
}
