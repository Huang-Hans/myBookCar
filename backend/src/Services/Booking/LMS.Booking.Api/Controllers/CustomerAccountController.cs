using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.CustomerRecords;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.CustomerRecords;
using LMS.Booking.Application.Customers.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客户账户资料与偏好接口。客户标识只来自令牌；账户资料独立于预订资格，Platform 规则或客户限制来源不可用时仍可读写。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[Tags("客户账户")]
[Route("api/booking/customer/v1/me")]
public sealed class CustomerAccountController(
    ICustomerAccountHandler accountHandler,
    ICustomerPreferencesHandler preferencesHandler) : ControllerBase
{
    /// <summary>
    /// C2-01 查询本人账户资料与会员摘要。
    /// </summary>
    /// <remarks>
    /// 会员号、等级、积分来自会员快照，只读且不是实时余额；没有可信积分时省略。登录手机号只读（掩码）。响应不缓存。
    /// </remarks>
    /// <returns>账户资料。</returns>
    [HttpGet("profile", Name = "getCustomerProfile")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerAccountView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerProfileRead)]
    public async Task<IResult> GetProfileAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await accountHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-02 修改本人账户普通资料（姓名、邮箱）。
    /// </summary>
    /// <remarks>
    /// 只改本地补充资料，与会员快照分开保存，快照导入不会覆盖。会员号、等级、积分、物业、登录手机号不能通过本接口修改。
    /// 可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409。
    /// </remarks>
    /// <returns>修改后的账户资料。</returns>
    [HttpPatch("profile", Name = "updateCustomerProfile")]
    [ApiRequestBody(typeof(UpdateCustomerAccountRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerAccountView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerProfileWrite)]
    public async Task<IResult> UpdateProfileAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<UpdateCustomerAccountRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = CustomerRecordMapper.ToCommand(
            HttpContext.GetPrincipal().EntityId, version.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await accountHandler.UpdateAsync(command.Value, cancellationToken), CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-10 查询本人偏好。
    /// </summary>
    /// <remarks>
    /// 从未保存过时返回契约默认（language=EN，推送关闭，version=0），不写库。delivery.isPushDeliveryAvailable 说明实际推送能力（目前未接入）。响应不缓存。
    /// </remarks>
    /// <returns>本人偏好。</returns>
    [HttpGet("preferences", Name = "getCustomerPreferences")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerPreferencesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerPreferencesRead)]
    public async Task<IResult> GetPreferencesAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await preferencesHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-11 修改本人偏好。
    /// </summary>
    /// <remarks>
    /// 只允许声明的字段（语言、推送总开关）。可选 X-Expected-Version：提供时必须等于当前版本，否则 409。内容没有变化时不写入。
    /// 保存推送开关不代表设备已订阅，也不修改会员积分或已有订单的确认语言。
    /// </remarks>
    /// <returns>保存后的偏好。</returns>
    [HttpPatch("preferences", Name = "updateCustomerPreferences")]
    [ApiRequestBody(typeof(UpdateCustomerPreferencesRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerPreferencesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerPreferencesWrite)]
    public async Task<IResult> UpdatePreferencesAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<UpdateCustomerPreferencesRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = CustomerRecordMapper.ToCommand(
            HttpContext.GetPrincipal().EntityId, version.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await preferencesHandler.UpdateAsync(command.Value, cancellationToken), CustomerRecordMapper.ToView);
    }
}
