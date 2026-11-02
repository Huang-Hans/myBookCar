using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Internal;
using LMS.Dispatch.Application.Credentials.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 服务间接口：Platform 的司机登录与刷新核对司机凭据。
/// </summary>
/// <remarks>
/// 不经网关；只接受 Platform 签发的短效服务令牌（PlatformService 策略），员工、司机、客户令牌都不能调用。
/// 响应只包含核对结论与 driverId，不返回密码哈希，也不区分失败原因。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.PlatformService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalDriverCredentialsController(
    IVerifyDriverCredentialsHandler verifyHandler,
    ICheckDriverLoginEligibilityHandler eligibilityHandler) : ControllerBase
{
    /// <summary>
    /// 核对司机登录凭据；核对结论在 data.isVerified。
    /// </summary>
    /// <returns>核对通过时带有 driverId。</returns>
    [HttpPost("driver-credential-verifications")]
    [ApiRequestBody(typeof(DriverCredentialVerificationRequest))]
    public async Task<IResult> VerifyAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<DriverCredentialVerificationRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        if (string.IsNullOrWhiteSpace(body.Value.Phone) || string.IsNullOrEmpty(body.Value.Password))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("phone", "phone and password are required")]);
        }

        var result = await verifyHandler.HandleAsync(body.Value.Phone, body.Value.Password, cancellationToken);

        return ApiResults.Ok(new CredentialVerificationView(result.IsVerified, result.DriverId));
    }

    /// <summary>
    /// 核对司机是否仍可登录或刷新令牌。
    /// </summary>
    /// <param name="driverId">司机标识。</param>
    /// <returns>是否可登录。</returns>
    [HttpGet("driver-login-eligibilities/{driverId}")]
    public async Task<IResult> CheckEligibilityAsync(string driverId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return ApiResults.Ok(new LoginEligibilityView(await eligibilityHandler.HandleAsync(driverId, cancellationToken)));
    }
}
