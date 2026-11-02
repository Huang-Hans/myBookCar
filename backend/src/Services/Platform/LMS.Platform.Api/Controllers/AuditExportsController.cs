using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Exports;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Exports;
using LMS.Platform.Application.Exports.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 审计 Excel 导出接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("审计导出")]
[Route("api/platform/staff/v1/audit-exports")]
public sealed class AuditExportsController(
    ICreateAuditExportHandler createHandler,
    IAuditExportQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// AX01 申请审计 Excel 导出。
    /// </summary>
    /// <remarks>
    /// 只受理（202 / 20200），文件由后台作业生成；用 AX02 轮询状态。导出内容为申请时刻之前、筛选范围内的审计记录。
    /// 复用审计列表的 audit:read 权限。
    /// </remarks>
    /// <returns>受理的导出作业，初始状态 QUEUED；相同幂等键且内容一致的重放返回该作业的当前状态。</returns>
    [HttpPost(Name = "createAuditExport")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateAuditExportRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ExportOperationView>), StatusCodes.Status202Accepted)]
    [RequirePermission(PermissionCodes.AuditRead)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateAuditExportRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = AuditExportRequestMapper.ToCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToAcceptedResult(await createHandler.HandleAsync(command.Value, cancellationToken), ExportViewMapper.ToView);
    }

    /// <summary>
    /// AX02 读取审计导出状态。
    /// </summary>
    /// <param name="exportId">导出标识。</param>
    /// <returns>导出作业状态；不在调用者当前授权范围内或不存在时返回 404。</returns>
    [HttpGet("{exportId}", Name = "getAuditExport")]
    [ProducesResponseType(typeof(ApiEnvelope<ExportOperationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.AuditRead)]
    public async Task<IResult> GetAsync(string exportId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(exportId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ExportViewMapper.ToView);
    }

    /// <summary>
    /// AX03 取得审计授权下载描述。
    /// </summary>
    /// <remarks>
    /// 签发时按调用者当前授权复核；返回的地址短效且自带签名，已签发的地址在失效前仍可使用。
    /// </remarks>
    /// <param name="exportId">导出标识。</param>
    /// <returns>下载描述；作业尚未完成或结果已过期返回 422 / 42200，不在授权范围内或不存在返回 404。</returns>
    [HttpGet("{exportId}/download-descriptions", Name = "getAuditExportDownloadDescription")]
    [ProducesResponseType(typeof(ApiEnvelope<DownloadDescriptionView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.AuditRead)]
    public async Task<IResult> DescribeDownloadAsync(string exportId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.DescribeDownloadAsync(exportId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ExportViewMapper.ToView);
    }
}
