using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Reporting.Api.Contracts.Exports;
using LMS.Reporting.Api.Controllers.Support;
using LMS.Reporting.Api.Mapping.Exports;
using LMS.Reporting.Application.Exports.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Reporting.Api.Controllers;

/// <summary>
/// 运营报表文件导出接口。
/// </summary>
/// <remarks>
/// 导出复用报表预览的同一读取口径与授权。申请只受理（202 / 20200），文件由后台作业生成；报表来源（Q04）未接入时不受理，返回 503 / 50300。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("报表导出")]
[Route("api/reporting/staff/v1/report-exports")]
public sealed class ReportExportsController(
    ICreateReportExportHandler createHandler,
    IReportExportQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// EX01 申请报表文件导出。
    /// </summary>
    /// <remarks>
    /// 目前只支持 XLSX；PDF 的服务端生成尚未实现，传 PDF 返回 400 / 40001。用 EX02 轮询状态。
    /// </remarks>
    /// <returns>受理的导出作业，初始状态 QUEUED；相同幂等键且内容一致的重放返回该作业的当前状态。</returns>
    [HttpPost(Name = "createReportExport")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateReportExportRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ExportOperationView>), StatusCodes.Status202Accepted)]
    [RequirePermission(PermissionCodes.ReportsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateReportExportRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ReportExportMapper.ToCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToAcceptedResult(await createHandler.HandleAsync(command.Value, cancellationToken), ReportExportMapper.ToView);
    }

    /// <summary>
    /// EX02 读取导出结果状态。
    /// </summary>
    /// <param name="exportId">导出标识。</param>
    /// <returns>导出作业状态；不在调用者当前授权范围内或不存在时返回 404。</returns>
    [HttpGet("{exportId}", Name = "getReportExport")]
    [ProducesResponseType(typeof(ApiEnvelope<ExportOperationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public async Task<IResult> GetAsync(string exportId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(exportId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ReportExportMapper.ToView);
    }

    /// <summary>
    /// EX03 取得报表导出的授权下载描述。
    /// </summary>
    /// <remarks>
    /// 签发时按调用者当前授权复核；返回的地址短效且自带签名，已签发的地址在失效前仍可使用。
    /// </remarks>
    /// <param name="exportId">导出标识。</param>
    /// <returns>下载描述；作业尚未完成或结果已过期返回 422 / 42200，不在授权范围内或不存在返回 404。</returns>
    [HttpGet("{exportId}/download-descriptions", Name = "getReportExportDownloadDescription")]
    [ProducesResponseType(typeof(ApiEnvelope<DownloadDescriptionView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ReportsRead)]
    public async Task<IResult> DescribeDownloadAsync(string exportId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.DescribeDownloadAsync(exportId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ReportExportMapper.ToView);
    }
}
