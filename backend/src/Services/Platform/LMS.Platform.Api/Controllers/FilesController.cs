using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Files;
using LMS.Platform.Api.Mapping.Files;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Handlers;
using LMS.Platform.Application.Files.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 文件元数据与内容上传接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("文件")]
[Route("api/platform/staff/v1")]
public sealed class FilesController(
    IFileQueryHandler queryHandler,
    IUploadFileContentHandler uploadHandler,
    IDownloadFileContentHandler downloadHandler,
    IOptions<FileStorageOptions> storageOptions) : ControllerBase
{
    /// <summary>
    /// FI01 查询文件元数据。
    /// </summary>
    /// <param name="fileId">文件标识。</param>
    /// <returns>文件元数据；文件不属于调用者物业范围时按不可见处理。</returns>
    [HttpGet("files/{fileId}", Name = "getFile")]
    [ProducesResponseType(typeof(ApiEnvelope<FileMetadataView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> GetAsync(string fileId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(fileId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, FileViewMapper.ToView);
    }

    /// <summary>
    /// FI02 上传文件内容。
    /// </summary>
    /// <remarks>
    /// 补充端点：不在 76 个合同操作内，FI02 只作引用编号。由短效签名 URL 授权（不使用 Bearer），
    /// 签名只覆盖单个文件和失效时间；CB05 返回的上传地址即指向本接口。
    /// </remarks>
    /// <param name="fileId">文件标识。</param>
    /// <returns>上传后的文件元数据；签名无效或过期时返回 403。</returns>
    [AllowAnonymous]
    [HttpPut("file-uploads/{fileId}", Name = "uploadFileContent")]
    [ApiRequestBody(typeof(Stream), "*/*")]
    [ProducesResponseType(typeof(ApiEnvelope<FileMetadataView>), StatusCodes.Status200OK)]
    public async Task<IResult> UploadAsync(string fileId, [FromQuery] FileUploadQuery query, CancellationToken cancellationToken)
    {
        LimitBody(storageOptions.Value.MaxSizeBytes);
        var command = new UploadFileContentCommand(fileId, query.Expires!.Value, query.Signature!, Request.ContentType, Request.Body);

        return OperationResultMapper.ToResult(await uploadHandler.HandleAsync(command, cancellationToken), FileViewMapper.ToView);
    }

    /// <summary>
    /// 下载导出结果文件。
    /// </summary>
    /// <remarks>
    /// 补充端点：由 AX03 返回的短效签名 URL 授权（不使用 Bearer），签名只覆盖单个文件和失效时间；
    /// 只提供导出结果文件，审批材料不经此入口下载。成功时响应体是文件内容，不是统一 Envelope。
    /// </remarks>
    /// <param name="fileId">文件标识。</param>
    /// <returns>文件内容；签名无效或过期返回 403，文件不存在或导出结果已过期返回 404。</returns>
    [AllowAnonymous]
    [HttpGet("file-downloads/{fileId}", Name = "downloadFileContent")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IResult> DownloadAsync(string fileId, [FromQuery] FileDownloadQuery query, CancellationToken cancellationToken)
    {
        var command = new DownloadFileContentCommand(fileId, query.Expires!.Value, query.Signature!);
        var result = await downloadHandler.HandleAsync(command, cancellationToken);

        return result.IsSuccess
            ? Results.File(result.Value!.Content, result.Value.ContentType, result.Value.FileName)
            : OperationResultMapper.ToFailure(result);
    }

    private void LimitBody(long maxBytes)
    {
        var feature = HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (feature is { IsReadOnly: false } && maxBytes > 0)
        {
            feature.MaxRequestBodySize = maxBytes + 1;
        }
    }
}
