using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Approvals;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Approvals;
using LMS.Booking.Application.Approvals.Handlers;
using LMS.Booking.Domain.Approvals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 跨境审批、审核材料与批量决定接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("跨境审批")]
[Route("api/booking/staff/v1")]
public sealed class CrossBorderApprovalsController(
    IApprovalQueryHandler queryHandler,
    IUpdateApprovalDocumentHandler documentHandler,
    IRegisterApprovalUploadHandler uploadHandler,
    IDecideApprovalHandler decideHandler,
    IBulkDecideApprovalsHandler bulkDecideHandler) : ControllerBase
{
    /// <summary>
    /// CB01 分页查询跨境审批。
    /// </summary>
    /// <returns>分页的跨境审批列表；分页信息在 meta.pagination。</returns>
    [HttpGet("cross-border-approvals", Name = "listCrossBorderApprovals")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CrossBorderApprovalView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> ListAsync([FromQuery] ApprovalListRequest request, CancellationToken cancellationToken)
    {
        var query = ApprovalRequestMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB03 查询审批汇总。
    /// </summary>
    /// <remarks>
    /// 与列表查询共用同一套筛选，不分页。
    /// </remarks>
    /// <returns>待处理数量与审批链路汇总卡片。</returns>
    [HttpGet("cross-border-approval-summaries", Name = "getCrossBorderApprovalSummary")]
    [ProducesResponseType(typeof(ApiEnvelope<ApprovalSummaryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> SummarizeAsync([FromQuery] ApprovalSummaryRequest request, CancellationToken cancellationToken)
    {
        var query = ApprovalRequestMapper.ToSummaryQuery(request, HttpContext.GetCaller().Scope);

        return OperationResultMapper.ToResult(await queryHandler.SummarizeAsync(query, cancellationToken), ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB02 查询审批详情。
    /// </summary>
    /// <param name="approvalId">审批标识。</param>
    /// <returns>审批详情，含路线快照、材料与黑名单命中。</returns>
    [HttpGet("cross-border-approvals/{approvalId}", Name = "getCrossBorderApproval")]
    [ProducesResponseType(typeof(ApiEnvelope<CrossBorderApprovalView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsRead)]
    public async Task<IResult> GetAsync(string approvalId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(approvalId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB04 更新审核材料。
    /// </summary>
    /// <param name="approvalId">审批标识。</param>
    /// <param name="documentType">材料类型枚举的对外字面值。</param>
    /// <returns>更新后的审批；材料不属于该审批或安全状态不是 CLEAN 时返回 422 / 42200。</returns>
    [HttpPatch("cross-border-approvals/{approvalId}/documents/{documentType}", Name = "updateApprovalDocument")]
    [ApiRequestBody(typeof(ApprovalDocumentPatchRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CrossBorderApprovalView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsMaterials)]
    public async Task<IResult> UpdateDocumentAsync(string approvalId, string documentType, CancellationToken cancellationToken)
    {
        if (!WireEnum.TryParse<ApprovalDocumentType>(documentType, out var type))
        {
            return InvalidDocumentType();
        }

        var body = await JsonRequestBody.ReadAsync<ApprovalDocumentPatchRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = ApprovalRequestMapper.ToDocumentCommand(approvalId, type, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await documentHandler.HandleAsync(command.Value, cancellationToken), ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB05 登记审核材料上传。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <param name="approvalId">审批标识。</param>
    /// <param name="documentType">材料类型枚举的对外字面值。</param>
    /// <returns>上传说明（含短效签名地址）；相同幂等键且内容一致的重放同样返回 201。文件存储未配置时返回 503 / 50300。</returns>
    [HttpPost("cross-border-approvals/{approvalId}/documents/{documentType}/uploads", Name = "registerApprovalDocumentUpload")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(FileUploadRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<UploadDescriptionView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.ApprovalsMaterials)]
    public async Task<IResult> RegisterUploadAsync(string approvalId, string documentType, CancellationToken cancellationToken)
    {
        if (!WireEnum.TryParse<ApprovalDocumentType>(documentType, out var type))
        {
            return InvalidDocumentType();
        }

        var body = await JsonRequestBody.ReadAsync<FileUploadRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ApprovalRequestMapper.ToUploadCommand(approvalId, type, body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await uploadHandler.HandleAsync(command.Value, cancellationToken), ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB06 批准或拒绝跨境审批。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <param name="approvalId">审批标识。</param>
    /// <returns>决定后的审批；材料未全部核验、客户存在有效限制或司机不可用时返回 422 / 42200。</returns>
    [HttpPost("cross-border-approvals/{approvalId}/decisions", Name = "decideCrossBorderApproval")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(ApprovalDecisionRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CrossBorderApprovalView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsDecide)]
    public async Task<IResult> DecideAsync(string approvalId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ApprovalDecisionRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, version, key) is { } failure)
        {
            return failure;
        }

        var command = ApprovalRequestMapper.ToDecideCommand(approvalId, version.Value, body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await decideHandler.HandleAsync(command.Value, cancellationToken), ApprovalViewMapper.ToView);
    }

    /// <summary>
    /// CB07 批量决定跨境审批。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <returns>逐条决定结果；条目级的失败在 items 中给出，字段路径形如 items[0].approvalId。</returns>
    [HttpPost("cross-border-approval-decisions", Name = "bulkDecideCrossBorderApprovals")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(ApprovalBulkDecisionRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ApprovalBulkDecisionView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.ApprovalsDecide)]
    public async Task<IResult> BulkDecideAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ApprovalBulkDecisionRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = ApprovalRequestMapper.ToBulkCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await bulkDecideHandler.HandleAsync(command.Value, cancellationToken), ApprovalViewMapper.ToView);
    }

    // 列表与汇总共用的筛选：物业、状态、取货时间区间（from 含，to 不含，from 必须早于 to）。
    private static IResult InvalidDocumentType() =>
        ApiResults.ValidationFailed([new ApiFieldError("documentType", $"Must be one of: {string.Join(", ", WireEnum.WireNames<ApprovalDocumentType>())}")]);
}
