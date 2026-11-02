using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.Platform.Api.Contracts.AppAuthentication;
using LMS.Platform.Api.Controllers.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 司机登录、刷新与退出。司机凭据由 Dispatch 的司机记录核对，令牌由 LMS 本地身份服务签发，不经过 Keycloak。
/// </summary>
/// <remarks>
/// 登录类型由入口固定为 DRIVER。司机刷新凭据不能在客户入口使用，也不能换取客户令牌。
/// 本期不提供注册、激活、改密或验证码登录；尚未设置密码的司机不能登录。
/// </remarks>
[ApiController]
[AllowAnonymous]
[Tags("司机认证")]
[Route("api/platform/driver/v1")]
public sealed class DriverAuthenticationController(AppAuthenticationFlow flow) : ControllerBase
{
    /// <summary>司机登录。</summary>
    /// <remarks>手机号密码登录：phone 可带国家/地区码（如 +85361234567），也可直接使用本地号码（11 位中国手机号补 +86、8 位澳门号码补 +853），规范化后与 drivers.phone 精确匹配。手机号不存在或重复、密码错误、密码未设置或司机不是 ACTIVE 状态都返回同样的 40100。</remarks>
    /// <returns>访问令牌与刷新令牌。</returns>
    [HttpPost("logins", Name = "createDriverLogin")]
    [ApiRequestBody(typeof(AppLoginRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<AppSessionView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public Task<IResult> LoginAsync(CancellationToken cancellationToken) =>
        flow.LoginAsync(HttpContext, PrincipalType.Driver, cancellationToken);

    /// <summary>用司机刷新令牌换取新的访问令牌与刷新令牌。</summary>
    /// <remarks>刷新令牌使用即轮换，新的刷新令牌沿用原登录起 7 天的绝对到期时间。已使用过的刷新令牌再次提交会撤销整条刷新链。</remarks>
    /// <returns>新的访问令牌与刷新令牌。</returns>
    [HttpPost("token-refreshes", Name = "createDriverTokenRefresh")]
    [ApiRequestBody(typeof(AppRefreshRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<AppSessionView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public Task<IResult> RefreshAsync(CancellationToken cancellationToken) =>
        flow.RefreshAsync(HttpContext, PrincipalType.Driver, cancellationToken);

    /// <summary>司机退出：撤销当前刷新链。</summary>
    /// <remarks>重复退出同样成功。已签发的访问令牌在到期前（最长 15 分钟）仍可使用，本期不提供即时撤销。</remarks>
    /// <returns>成功时无响应体，只返回 204。</returns>
    [HttpPost("logouts", Name = "createDriverLogout")]
    [ApiRequestBody(typeof(AppLogoutRequest))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public Task<IResult> LogoutAsync(CancellationToken cancellationToken) =>
        flow.LogoutAsync(HttpContext, PrincipalType.Driver, cancellationToken);
}
