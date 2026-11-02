using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Internal;
using LMS.Booking.Application.Credentials.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 服务间接口：Platform 的客户登录与刷新核对客户凭据。
/// </summary>
/// <remarks>
/// 不经网关；只接受 Platform 签发的短效服务令牌（PlatformService 策略），员工、司机、客户令牌都不能调用。
/// 响应只包含核对结论与 guestId，不返回密码哈希，也不区分失败原因。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.PlatformService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalCustomerCredentialsController(
    IVerifyCustomerCredentialsHandler verifyHandler,
    ICheckCustomerLoginEligibilityHandler eligibilityHandler) : ControllerBase
{
    /// <summary>
    /// 核对客户登录凭据；核对结论在 data.isVerified。
    /// </summary>
    /// <returns>核对通过时带有 guestId。</returns>
    [HttpPost("customer-credential-verifications")]
    [ApiRequestBody(typeof(CustomerCredentialVerificationRequest))]
    public async Task<IResult> VerifyAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CustomerCredentialVerificationRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        if (string.IsNullOrWhiteSpace(body.Value.Phone) || string.IsNullOrEmpty(body.Value.Password))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("phone", "phone and password are required")]);
        }

        var result = await verifyHandler.HandleAsync(body.Value.Phone, body.Value.Password, cancellationToken);

        return ApiResults.Ok(new CredentialVerificationView(result.IsVerified, result.GuestId));
    }

    /// <summary>
    /// 核对客户是否仍可登录或刷新令牌。
    /// </summary>
    /// <param name="guestId">宾客标识。</param>
    /// <returns>是否可登录。</returns>
    [HttpGet("customer-login-eligibilities/{guestId}")]
    public async Task<IResult> CheckEligibilityAsync(string guestId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return ApiResults.Ok(new LoginEligibilityView(await eligibilityHandler.HandleAsync(guestId, cancellationToken)));
    }
}
