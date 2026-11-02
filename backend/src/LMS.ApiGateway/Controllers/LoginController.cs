using LMS.ApiGateway.Contracts.Authentication;
using LMS.ApiGateway.Authentication;
using LMS.ApiGateway.Controllers.Support;
using LMS.ApiGateway.Documentation;
using LMS.ApiGateway.Requests.Authentication;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Http.Tracing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.ApiGateway.Controllers;

/// <summary>通过已配置的身份提供方登录。</summary>
[ApiController]
[AllowAnonymous]
[Tags(GatewayApiTags.Authentication)]
// WINSON-DEVIATION: The explicitly requested login action uses a login route.
[Route(AuthenticationRoutes.Login)]
public sealed class LoginController(IGatewayLoginService loginService) : ControllerBase
{
    /// <summary>使用用户名和密码登录。</summary>
    /// <remarks>
    /// 网关向 Keycloak 转发密码授权请求，无需 Bearer 令牌。Keycloak 校验通过后，网关用新令牌请求 Platform 核对 LMS 用户：
    /// 令牌主体已关联，或首次登录时按已验证企业邮箱关联到同邮箱、未关联且启用的用户，才返回令牌；否则返回 40300，不返回令牌。
    /// </remarks>
    [HttpPost(Name = "login")]
    [ApiRequestBody(typeof(LoginRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status504GatewayTimeout)]
    public async Task<IResult> LoginAsync(CancellationToken cancellationToken)
    {
        Response.DisableCaching();
        var body = await JsonRequestBody.ReadAsync<LoginRequest>(HttpContext, cancellationToken);
        if (body.Failure is { } parseFailure)
        {
            return parseFailure;
        }

        if (LoginRequestValidation.Validate(body.Value) is { } validationFailure)
        {
            return validationFailure;
        }

        var result = await loginService.LoginAsync(body.Value, HttpContext.GetTraceId(), cancellationToken);

        return result.Error is { } error ? ApiResults.Fail(error) : ApiResults.Ok(result.Response!);
    }
}
