using LMS.ApiGateway.Contracts.Authentication;

namespace LMS.ApiGateway.Authentication;

public interface IGatewayLoginService
{
    // Signs in with the identity provider, then requires the subject to map to a usable LMS user.
    Task<LoginResult> LoginAsync(LoginRequest request, string traceId, CancellationToken cancellationToken);
}
