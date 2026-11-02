using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Results;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace LMS.BuildingBlocks.Identity.Authentication;

internal static class JwtBearerChallengeHandler
{
    // 令牌缺失或无效为 40100；可识别的令牌过期为 40101。响应体不暴露校验失败的内部原因。
    public static Task OnChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        var error = context.AuthenticateFailure is SecurityTokenExpiredException
            ? ApiErrors.TokenExpired
            : ApiErrors.NotAuthenticated;
        context.Response.Headers.WWWAuthenticate = "Bearer";

        return ApiErrorWriter.WriteAsync(context.HttpContext, error);
    }
}
