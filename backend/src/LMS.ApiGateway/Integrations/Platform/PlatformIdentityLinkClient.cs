using System.Net;
using System.Net.Http.Headers;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;

namespace LMS.ApiGateway.Integrations.Platform;

internal sealed class PlatformIdentityLinkClient(HttpClient client) : IPlatformIdentityLinkClient
{
    private const string IdentityLinksPath = "internal/v1/identity-links";
    private const string BearerScheme = "Bearer";

    private static readonly ApiError BadGateway = ApiErrors.FromStatusCode(StatusCodes.Status502BadGateway);
    private static readonly ApiError GatewayTimeout = ApiErrors.FromStatusCode(StatusCodes.Status504GatewayTimeout);

    public async Task<ApiError?> LinkAsync(string accessToken, string traceId, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(accessToken, traceId);
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            return ToError(response.StatusCode);
        }
        catch (Exception exception) when (IsUpstreamFailure(exception, cancellationToken))
        {
            return exception is OperationCanceledException ? GatewayTimeout : BadGateway;
        }
    }

    private static HttpRequestMessage CreateRequest(string accessToken, string traceId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, IdentityLinksPath);
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, accessToken);
        request.Headers.Add(ApiHeaderNames.TraceId, traceId);

        return request;
    }

    // 403 means the subject has no usable LMS user; any other failure is an upstream fault, never a permission verdict.
    private static ApiError? ToError(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.OK => null,
        HttpStatusCode.Forbidden => ApiErrors.InsufficientPermission,
        HttpStatusCode.ServiceUnavailable => ApiErrors.ServiceUnavailable,
        _ => BadGateway
    };

    private static bool IsUpstreamFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or OperationCanceledException && !cancellationToken.IsCancellationRequested;
}
