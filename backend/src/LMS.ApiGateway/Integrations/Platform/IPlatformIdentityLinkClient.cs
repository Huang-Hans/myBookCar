using LMS.BuildingBlocks.Http.Contracts;

namespace LMS.ApiGateway.Integrations.Platform;

public interface IPlatformIdentityLinkClient
{
    // Checks that the token subject maps to a usable LMS user, linking it on first sign-in.
    // Returns null when the subject may use LMS; otherwise the error to return from login.
    Task<ApiError?> LinkAsync(string accessToken, string traceId, CancellationToken cancellationToken);
}
