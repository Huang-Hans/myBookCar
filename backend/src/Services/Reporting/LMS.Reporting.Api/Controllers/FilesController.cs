using LMS.BuildingBlocks.Http.Results;
using LMS.Reporting.Api.Contracts.Files;
using LMS.Reporting.Application.Files.Commands;
using LMS.Reporting.Application.Files.Handlers;
using LMS.BuildingBlocks.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Reporting.Api.Controllers;

/// <summary>
/// 导出结果文件下载接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("文件")]
[Route("api/reporting/staff/v1")]
public sealed class FilesController(IDownloadFileContentHandler downloadHandler) : ControllerBase
{
    /// <summary>
    /// 下载报表导出结果文件。
    /// </summary>
    /// <remarks>
    /// 补充端点：由 EX03 返回的短效签名 URL 授权（不使用 Bearer），签名只覆盖单个文件和失效时间。
    /// 成功时响应体是文件内容，不是统一 Envelope。
    /// </remarks>
    /// <param name="fileId">文件标识。</param>
    /// <returns>文件内容；签名无效或过期返回 403，文件不存在或导出结果已过期返回 404。</returns>
    [AllowAnonymous]
    [HttpGet("file-downloads/{fileId}", Name = "downloadReportFile")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IResult> DownloadAsync(string fileId, [FromQuery] FileDownloadQuery query, CancellationToken cancellationToken)
    {
        var command = new DownloadFileContentCommand(fileId, query.Expires!.Value, query.Signature!);
        var result = await downloadHandler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
            : OperationResultMapper.ToFailure(result);
    }
}
