using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Files;
using LMS.Platform.Api.Controllers.Support;
using LMS.Platform.Api.Mapping.Files;
using LMS.Platform.Application.Files.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间接口：Booking 登记/读取审核材料文件。
/// </summary>
/// <remarks>
/// 不经网关；均转发终端用户的令牌，并重新检查权限与物业范围。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalFilesController(
    IRegisterFileHandler registerHandler,
    IFileQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// 登记待上传的文件。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>登记结果与上传说明（含签名地址）。</returns>
    [HttpPost("files")]
    [RequirePermission(PermissionCodes.ApprovalsMaterials)]
    public async Task<IResult> RegisterAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<RegisterFileInternalRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = FileRequestMapper.ToCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await registerHandler.HandleAsync(command.Value, cancellationToken), FileViewMapper.ToView);
    }

    /// <summary>
    /// 读取文件元数据。
    /// </summary>
    /// <param name="fileId">文件标识。</param>
    /// <returns>文件元数据；不属于调用者物业范围时按不可见处理。</returns>
    [HttpGet("files/{fileId}")]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> GetAsync(string fileId, CancellationToken cancellationToken)
    {
        var result = await queryHandler.GetAsync(fileId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, FileViewMapper.ToView);
    }
}
