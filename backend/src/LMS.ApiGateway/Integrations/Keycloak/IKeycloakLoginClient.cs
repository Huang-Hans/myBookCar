using LMS.ApiGateway.Contracts.Authentication;

namespace LMS.ApiGateway.Integrations.Keycloak;

public interface IKeycloakLoginClient
{
    Task<LoginResult> LoginAsync(LoginRequest request, string traceId, CancellationToken cancellationToken);

    // Exchanges a refresh token for a new token set; the result has the same shape as a login.
    Task<LoginResult> RefreshAsync(RefreshTokenRequest request, string traceId, CancellationToken cancellationToken);
}
