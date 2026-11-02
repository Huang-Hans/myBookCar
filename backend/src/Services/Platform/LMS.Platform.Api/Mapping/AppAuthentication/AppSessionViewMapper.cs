using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Api.Contracts.AppAuthentication;
using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Api.Mapping.AppAuthentication;

internal static class AppSessionViewMapper
{
    private const string BearerTokenType = "Bearer";

    public static AppSessionView ToView(AppSession session) => new(
        session.AccessToken,
        BearerTokenType,
        session.ExpiresInSeconds,
        session.RefreshToken,
        session.RefreshExpiresInSeconds,
        WireEnum.ToWire(session.PrincipalType));
}
