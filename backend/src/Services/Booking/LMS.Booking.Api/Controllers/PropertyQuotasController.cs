using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Quotas;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Quotas;
using LMS.Booking.Application.Quotas.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 物业每日限额接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("物业限额")]
public sealed class PropertyQuotasController(
    IPropertyQuotaQueryHandler queryHandler,
    IUpdatePropertyQuotaHandler updateHandler) : ControllerBase
{
    /// <summary>
    /// QT01 分页查询物业限额。
    /// </summary>
    /// <returns>分页的物业限额列表；分页信息在 meta.pagination。</returns>
    [HttpGet("api/booking/staff/v1/property-quotas", Name = "listPropertyQuotas")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<PropertyQuotaView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QuotasRead)]
    public async Task<IResult> ListAsync([FromQuery] PropertyQuotaListRequest request, CancellationToken cancellationToken)
    {
        var query = QuotaMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, QuotaMapper.ToView);
    }

    /// <summary>
    /// QT02 查询物业限额详情。
    /// </summary>
    /// <param name="propertyId">物业标识。</param>
    /// <returns>物业限额详情。</returns>
    [HttpGet("api/booking/staff/v1/property-quotas/{propertyId}", Name = "getPropertyQuota")]
    [ProducesResponseType(typeof(ApiEnvelope<PropertyQuotaView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QuotasRead)]
    public async Task<IResult> GetAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(propertyId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, QuotaMapper.ToView);
    }

    /// <summary>
    /// QT03 部分更新物业限额。
    /// </summary>
    /// <param name="propertyId">物业标识。</param>
    /// <returns>更新后的限额；配额已用量未知时返回 503 / 50300，不使用估算值。</returns>
    [HttpPatch("api/booking/staff/v1/property-quotas/{propertyId}", Name = "updatePropertyQuota")]
    [ApiRequestBody(typeof(PatchPropertyQuotaRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<PropertyQuotaView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QuotasWrite)]
    public async Task<IResult> UpdateAsync(string propertyId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchPropertyQuotaRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = QuotaMapper.ToUpdateCommand(propertyId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await updateHandler.HandleAsync(command.Value, cancellationToken), QuotaMapper.ToView);
    }

    /// <summary>
    /// 服务间接口：Platform 在物业配置读取中组合权威每日限额。
    /// </summary>
    /// <remarks>
    /// 不经网关。
    /// </remarks>
    /// <returns>调用者物业范围内的每日限额列表。</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("internal/v1/property-quotas/daily-quotas")]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public async Task<IResult> ListDailyQuotasAsync(CancellationToken cancellationToken)
    {
        var quotas = await queryHandler.ListDailyQuotasAsync(HttpContext.GetCaller().Scope, cancellationToken);

        return ApiResults.Ok(quotas.Select(QuotaMapper.ToView).ToList());
    }
}
