using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Preferences;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Preferences;
using LMS.Dispatch.Application.Preferences.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机本人偏好接口（目前只有界面语言）。司机标识只来自令牌。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机偏好")]
[Route("api/dispatch/driver/v1/me/preferences")]
public sealed class DriverPreferencesController(
    IDriverPreferencesQueryHandler queryHandler,
    IUpdateDriverPreferencesHandler updateHandler) : ControllerBase
{
    /// <summary>
    /// D2-08 查询本人偏好。
    /// </summary>
    /// <remarks>
    /// 从未保存过时返回契约默认（language=EN，version=0），不写库。响应不缓存。
    /// </remarks>
    /// <returns>本人偏好。</returns>
    [HttpGet(Name = "getDriverPreferences")]
    [ProducesResponseType(typeof(ApiEnvelope<DriverPreferencesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverPreferencesRead)]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, DriverPreferencesMapper.ToView);
    }

    /// <summary>
    /// D2-09 修改本人偏好。
    /// </summary>
    /// <remarks>
    /// 只允许声明的字段。可选 X-Expected-Version：提供时必须等于当前版本（从未保存过为 0），否则 409；首次保存可不带。
    /// 内容没有变化时不写入，返回当前记录。
    /// </remarks>
    /// <returns>保存后的偏好。</returns>
    [HttpPatch(Name = "updateDriverPreferences")]
    [ApiRequestBody(typeof(UpdateDriverPreferencesRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverPreferencesView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverPreferencesWrite)]
    public async Task<IResult> UpdateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<UpdateDriverPreferencesRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = DriverPreferencesMapper.ToCommand(
            HttpContext.GetPrincipal().EntityId, version.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await updateHandler.HandleAsync(command.Value, cancellationToken), DriverPreferencesMapper.ToView);
    }
}
