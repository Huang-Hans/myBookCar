using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Configuration;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;
using LMS.Platform.Application.Quotas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 物业目录接口。
/// </summary>
/// <remarks>
/// 物业目录按调用者的物业范围限定：非全物业调用者只看到自己的物业，列表与详情使用同一范围。
/// dailyQuota 取自 Booking 的权威限额，Booking 不可用时省略；物业本身仍由 Platform 发布。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("物业")]
[Route("api/platform/staff/v1/properties")]
public sealed class PropertiesController(
    IConfigurationListHandler<ConfiguredProperty> handler,
    IPropertyQuotaDirectory quotas,
    ICreatePropertyHandler createHandler,
    IUpdatePropertyHandler updateHandler,
    IDeletePropertyHandler deleteHandler) : ControllerBase
{
    /// <summary>
    /// CF01 分页查询物业。
    /// </summary>
    /// <returns>分页的物业列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listProperties")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<PropertyView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public async Task<IResult> ListAsync([FromQuery] PropertyListRequest request, CancellationToken cancellationToken)
    {
        var scope = HttpContext.GetCaller().Scope;
        var dailyQuotas = await quotas.GetDailyQuotasAsync(cancellationToken);

        return await ConfigurationControllerSupport.ListAsync(
            handler, request.ToListQuery(), item => scope.Allows(item.PropertyId) && MatchesStatus(item, request.Status),
            item => ConfigurationViewMapper.ToView(item, dailyQuotas), cancellationToken);
    }

    /// <summary>
    /// CF02 查询物业详情。
    /// </summary>
    /// <param name="propertyId">物业标识。</param>
    /// <returns>物业详情；超出调用者物业范围时按不可见处理。</returns>
    [HttpGet("{propertyId}", Name = "getProperty")]
    [ProducesResponseType(typeof(ApiEnvelope<PropertyView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ConfigurationRead)]
    public async Task<IResult> GetAsync(string propertyId, CancellationToken cancellationToken)
    {
        var scope = HttpContext.GetCaller().Scope;
        var dailyQuotas = await quotas.GetDailyQuotasAsync(cancellationToken);

        return await ConfigurationControllerSupport.FindAsync(
            HttpContext, propertyId, handler, item => ConfigurationViewMapper.ToView(item, dailyQuotas),
            item => scope.Allows(item.PropertyId) && !item.IsDeleted, cancellationToken);
    }

    private static bool MatchesStatus(ConfiguredProperty item, PropertyStatus? status) => status switch
    {
        PropertyStatus.Active => item.IsActive && !item.IsDeleted,
        PropertyStatus.Inactive => !item.IsActive && !item.IsDeleted,
        PropertyStatus.Deleted => item.IsDeleted,
        _ => true
    };

    /// <summary>CF03 新增物业；只有全物业范围的调用者可以创建。</summary>
    [HttpPost(Name = "createProperty")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreatePropertyRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<PropertyView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.PropertiesWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreatePropertyRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure) return failure;
        var command = PropertyRequestMapper.Create(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null) return command.Failure;
        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken),
            item => ConfigurationViewMapper.ToView(item, null));
    }

    /// <summary>CF04 部分修改物业；物业代码不可修改。</summary>
    [HttpPatch("{propertyId}", Name = "updateProperty")]
    [ApiRequestBody(typeof(PatchPropertyRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<PropertyView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.PropertiesWrite)]
    public async Task<IResult> UpdateAsync(string propertyId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchPropertyRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure) return failure;
        var command = PropertyRequestMapper.Patch(propertyId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null) return command.Failure;
        return OperationResultMapper.ToUpdatedResult(await updateHandler.HandleAsync(command.Value, cancellationToken),
            item => ConfigurationViewMapper.ToView(item, null));
    }

    /// <summary>CF05 逻辑删除没有业务引用的物业；保留历史配置和审计记录。</summary>
    [HttpDelete("{propertyId}", Name = "deleteProperty")]
    [ProducesResponseType(typeof(ApiEnvelope<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ApiErrorEnvelope), StatusCodes.Status503ServiceUnavailable)]
    [RequirePermission(PermissionCodes.PropertiesWrite)]
    public async Task<IResult> DeleteAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure) return failure;
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (version.Failure is not null) return version.Failure;
        var command = new DeletePropertyCommand(propertyId, version.Value, CallerContexts.From(HttpContext));
        return OperationResultMapper.ToUpdatedResult(await deleteHandler.HandleAsync(command, cancellationToken), value => value);
    }
}
