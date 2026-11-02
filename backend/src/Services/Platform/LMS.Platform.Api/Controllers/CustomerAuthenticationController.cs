using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.Platform.Api.Contracts.AppAuthentication;
using LMS.Platform.Api.Controllers.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 客户登录、刷新与退出。客户凭据由 Booking 的客户记录核对，令牌由 LMS 本地身份服务签发，不经过 Keycloak。
/// </summary>
/// <remarks>
/// 登录类型由入口固定为 CUSTOMER。客户刷新凭据不能在司机入口使用，也不能换取司机令牌；同一个人的司机记录与客户记录互相独立，不自动合并。
/// 本期不提供注册、会员身份关联或 Melco 登录交接；尚未设置密码的客户不能登录。
/// </remarks>
[ApiController]
[AllowAnonymous]
[Tags("客户认证")]
[Route("api/platform/customer/v1")]
public sealed class CustomerAuthenticationController(AppAuthenticationFlow flow) : ControllerBase
{
    /// <summary>客户登录。</summary>
    /// <remarks>手机号密码登录：phone 可带国家/地区码（如 +85361234567），也可直接使用本地号码（11 位中国手机号补 +86、8 位澳门号码补 +853），规范化后与 guests.phone 精确匹配；登录不检查积分、游玩、免费用车资格或配额。手机号不存在或重复、密码错误或密码未设置都返回同样的 40100。</remarks>
    /// <returns>访问令牌与刷新令牌。</returns>
    [HttpPost("logins", Name = "createCustomerLogin")]
    [ApiRequestBody(typeof(AppLoginRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<AppSessionView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public Task<IResult> LoginAsync(CancellationToken cancellationToken) =>
        flow.LoginAsync(HttpContext, PrincipalType.Customer, cancellationToken);

    /// <summary>用客户刷新令牌换取新的访问令牌与刷新令牌。</summary>
    /// <remarks>刷新令牌使用即轮换，新的刷新令牌沿用原登录起 7 天的绝对到期时间。已使用过的刷新令牌再次提交会撤销整条刷新链。</remarks>
    /// <returns>新的访问令牌与刷新令牌。</returns>
    [HttpPost("token-refreshes", Name = "createCustomerTokenRefresh")]
    [ApiRequestBody(typeof(AppRefreshRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<AppSessionView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    public Task<IResult> RefreshAsync(CancellationToken cancellationToken) =>
        flow.RefreshAsync(HttpContext, PrincipalType.Customer, cancellationToken);

    /// <summary>客户退出：撤销当前刷新链。</summary>
    /// <remarks>重复退出同样成功。已签发的访问令牌在到期前（最长 15 分钟）仍可使用，本期不提供即时撤销。</remarks>
    /// <returns>成功时无响应体，只返回 204。</returns>
    [HttpPost("logouts", Name = "createCustomerLogout")]
    [ApiRequestBody(typeof(AppLogoutRequest))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status400BadRequest)]
    public Task<IResult> LogoutAsync(CancellationToken cancellationToken) =>
        flow.LogoutAsync(HttpContext, PrincipalType.Customer, cancellationToken);
}
