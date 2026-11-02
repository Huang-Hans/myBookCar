using LMS.ApiGateway.Contracts.Authentication;
using LMS.ApiGateway.Integrations.Keycloak;
using LMS.ApiGateway.Integrations.Platform;

namespace LMS.ApiGateway.Authentication;

// Login succeeds only when both checks pass: Keycloak accepts the credentials, and Platform maps the
// token subject to an active LMS user (linking it by verified corporate email on first sign-in).
// Tokens are not returned when the LMS check fails.
internal sealed class GatewayLoginService(
    IKeycloakLoginClient keycloakClient,
    IPlatformIdentityLinkClient platformClient) : IGatewayLoginService
{
    public async Task<LoginResult> LoginAsync(LoginRequest request, string traceId, CancellationToken cancellationToken)
    {
        var result = await keycloakClient.LoginAsync(request, traceId, cancellationToken);
        if (result.Response is not { } tokens)
        {
            return result;
        }

        var linkError = await platformClient.LinkAsync(tokens.AccessToken, traceId, cancellationToken);

        return linkError is null ? result : LoginResult.Failure(linkError);
    }
}
