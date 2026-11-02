using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Application.Authentication.Services;

namespace LMS.Platform.Application.Authentication.Handlers;

// 司机/客户登录：凭据由数据所属服务核对（Dispatch / Booking），通过后开始新的刷新链并签发同类型令牌。
// 密码未设置、密码错误、登录标识不存在或重复、记录不可登录都对外表现为同一种失败，不泄露具体原因。
internal sealed class LoginAppPrincipalHandler(
    IAppCredentialVerifier credentials,
    IAppAccessTokenIssuer issuer,
    ILoginAttemptThrottle throttle,
    AppSessionStore sessions) : ILoginAppPrincipalHandler
{
    public async Task<AppAuthResult> HandleAsync(AppLoginCommand command, CancellationToken cancellationToken)
    {
        if (!issuer.IsAvailable)
        {
            return AppAuthResult.Failed(AppAuthOutcome.SourceUnavailable);
        }

        var key = ThrottleKey(command);
        if (throttle.IsBlocked(key))
        {
            return AppAuthResult.Failed(AppAuthOutcome.TooManyAttempts);
        }

        var check = await credentials.VerifyAsync(command.Type, command.Phone, command.Password, cancellationToken);
        if (check.Outcome != CredentialCheckOutcome.Verified)
        {
            return await RejectAsync(command, key, check, cancellationToken);
        }

        throttle.Reset(key);

        return AppAuthResult.Succeeded(await sessions.StartAsync(command.Type, check.EntityId!, command.TraceId, cancellationToken));
    }

    private async Task<AppAuthResult> RejectAsync(AppLoginCommand command, string key, CredentialCheck check, CancellationToken cancellationToken)
    {
        if (check.Outcome == CredentialCheckOutcome.Unavailable)
        {
            return AppAuthResult.Failed(AppAuthOutcome.SourceUnavailable);
        }

        throttle.RecordFailure(key);
        await sessions.RecordLoginFailureAsync(command.Type, command.TraceId, cancellationToken);

        return AppAuthResult.Failed(AppAuthOutcome.NotAuthenticated);
    }

    private static string ThrottleKey(AppLoginCommand command) =>
        $"{WireEnum.ToWire(command.Type)}:{command.Phone}";
}
