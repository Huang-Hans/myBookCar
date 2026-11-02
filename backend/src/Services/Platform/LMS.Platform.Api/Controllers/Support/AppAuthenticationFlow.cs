using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Security;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Http.Tracing;
using LMS.Platform.Api.Contracts.AppAuthentication;
using LMS.Platform.Api.Mapping.AppAuthentication;
using LMS.Platform.Application.Authentication.Handlers;
using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Api.Controllers.Support;

// 司机与客户认证入口共用的请求处理：读取并校验正文，交给应用层，再把结果映射为统一响应。
// 身份类型由调用的 Controller 传入，请求正文不能选择身份。所有响应都禁止缓存。
public sealed class AppAuthenticationFlow(
    ILoginAppPrincipalHandler loginHandler,
    IRefreshAppSessionHandler refreshHandler,
    ILogoutAppSessionHandler logoutHandler)
{
    private const int RetryAfterSeconds = 60;

    public async Task<IResult> LoginAsync(HttpContext http, PrincipalType type, CancellationToken cancellationToken)
    {
        http.Response.DisableCaching();
        var body = await JsonRequestBody.ReadAsync<AppLoginRequest>(http, cancellationToken);
        if ((body.Failure ?? AppAuthenticationRequestValidation.Validate(body.Value)) is { } failure)
        {
            return failure;
        }

        PhoneNumber.TryNormalize(body.Value.Phone, out var phone);
        var command = new AppLoginCommand(type, phone, body.Value.Password, http.GetTraceId());

        return ToResult(http, await loginHandler.HandleAsync(command, cancellationToken));
    }

    public async Task<IResult> RefreshAsync(HttpContext http, PrincipalType type, CancellationToken cancellationToken)
    {
        http.Response.DisableCaching();
        var body = await JsonRequestBody.ReadAsync<AppRefreshRequest>(http, cancellationToken);
        if ((body.Failure ?? AppAuthenticationRequestValidation.ValidateRefreshToken(body.Value.RefreshToken)) is { } failure)
        {
            return failure;
        }

        var command = new AppRefreshCommand(type, body.Value.RefreshToken, http.GetTraceId());

        return ToResult(http, await refreshHandler.HandleAsync(command, cancellationToken));
    }

    public async Task<IResult> LogoutAsync(HttpContext http, PrincipalType type, CancellationToken cancellationToken)
    {
        http.Response.DisableCaching();
        var body = await JsonRequestBody.ReadAsync<AppLogoutRequest>(http, cancellationToken);
        if ((body.Failure ?? AppAuthenticationRequestValidation.ValidateRefreshToken(body.Value.RefreshToken)) is { } failure)
        {
            return failure;
        }

        var command = new AppLogoutCommand(type, body.Value.RefreshToken, http.GetTraceId());
        var result = await logoutHandler.HandleAsync(command, cancellationToken);

        return result.Outcome == AppAuthOutcome.Succeeded ? Results.NoContent() : ToResult(http, result);
    }

    private static IResult ToResult(HttpContext http, AppAuthResult result)
    {
        switch (result.Outcome)
        {
            case AppAuthOutcome.Succeeded:
                return ApiResults.Ok(AppSessionViewMapper.ToView(result.Session!));
            case AppAuthOutcome.TooManyAttempts:
                http.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
                return ApiResults.Fail(ApiErrors.TooManyRequests);
            case AppAuthOutcome.SourceUnavailable:
                return ApiResults.Fail(ApiErrors.ServiceUnavailable);
            default:
                return ApiResults.Fail(ApiErrors.NotAuthenticated);
        }
    }
}
