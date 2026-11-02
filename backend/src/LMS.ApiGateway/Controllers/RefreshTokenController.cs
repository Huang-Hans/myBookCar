using LMS.ApiGateway.Contracts.Authentication;
using LMS.ApiGateway.Controllers.Support;
using LMS.ApiGateway.Documentation;
using LMS.ApiGateway.Integrations.Keycloak;
using LMS.ApiGateway.Requests.Authentication;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.ApiGateway.Controllers;

/// <summary>通过已配置的身份提供方刷新令牌。</summary>
[ApiController]
[AllowAnonymous]
[Tags(GatewayApiTags.Authentication)]
// WINSON-DEVIATION: Refresh sits next to the explicitly requested login route and follows its naming.
[Route(AuthenticationRoutes.Refresh)]
public sealed class RefreshTokenController(IKeycloakLoginClient loginClient) : ControllerBase
{
    /// <summary>使用刷新令牌换取新的访问令牌。</summary>
    /// <remarks>
    /// 网关向 Keycloak 转发 refresh_token 授权请求。无需 Bearer 令牌：调用时访问令牌通常已经过期。
    /// Keycloak 可能轮换刷新令牌，调用方须保存本次返回的新刷新令牌。刷新令牌被拒绝时返回 40100，此时需要重新登录。
    /// </remarks>
    [HttpPost(Name = "refreshToken")]
    [ApiRequestBody(typeof(RefreshTokenRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status504GatewayTimeout)]
    public async Task<IResult> RefreshAsync(CancellationToken cancellationToken)
    {
        Response.DisableCaching();
        var body = await JsonRequestBody.ReadAsync<RefreshTokenRequest>(HttpContext, cancellationToken);
        if (body.Failure is { } parseFailure)
        {
            return parseFailure;
        }

        if (RefreshTokenRequestValidation.Validate(body.Value) is { } validationFailure)
        {
            return validationFailure;
        }

        var result = await loginClient.RefreshAsync(body.Value, HttpContext.GetTraceId(), cancellationToken);

        return result.Error is { } error ? ApiResults.Fail(error) : ApiResults.Ok(result.Response!);
    }
}
