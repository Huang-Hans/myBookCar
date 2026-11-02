using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Ports;
using LMS.Platform.Application.Authentication.Services;

namespace LMS.Platform.Application.Authentication.Handlers;

// 退出：撤销提交凭据所在的刷新链。凭据不存在、不属于当前入口身份或已撤销都按成功处理（幂等，且不暴露凭据是否存在）。
// 已签发的 Access Token 仍可用到过期，本期不做即时撤销。
internal sealed class LogoutAppSessionHandler(IAppRefreshTokenRepository tokens, AppSessionStore sessions) : ILogoutAppSessionHandler
{
    public async Task<AppAuthResult> HandleAsync(AppLogoutCommand command, CancellationToken cancellationToken)
    {
        var current = await tokens.FindByHashAsync(RefreshTokenSecret.HashOf(command.RefreshToken), cancellationToken);
        if (current is not null && current.PrincipalType == command.Type && current.RevokedAt is null)
        {
            await sessions.RevokeFamilyAsync(current, AppSessionAuditFactory.LogoutOperation, command.TraceId, cancellationToken);
        }

        return AppAuthResult.LoggedOut();
    }
}
