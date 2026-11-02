using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Qualifications;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Qualifications;
using LMS.Booking.Application.Qualifications.Commands;
using LMS.Booking.Application.Qualifications.Handlers;
using LMS.Booking.Domain.Qualifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 资格参考、资格例外升级与批准、资格审计接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("资格")]
public sealed class QualificationsController(
    IQualificationQueryHandler queryHandler,
    IEscalateQualificationHandler escalateHandler,
    IApproveQualificationExceptionHandler approveHandler,
    IRejectQualificationExceptionHandler rejectHandler,
    ICreateQualificationRequestHandler createHandler) : ControllerBase
{
    /// <summary>
    /// CQ01 分页查询资格参考。
    /// </summary>
    /// <returns>分页的资格参考列表；分页信息在 meta.pagination。</returns>
    [HttpGet("api/booking/staff/v1/qualification-references", Name = "listQualificationReferences")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<QualificationView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsRead)]
    public async Task<IResult> ListAsync([FromQuery] QualificationListRequest request, CancellationToken cancellationToken)
    {
        var query = QualificationMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, QualificationMapper.ToView);
    }

    /// <summary>
    /// CQ02 查询资格参考详情。
    /// </summary>
    /// <param name="qualificationId">资格参考标识。</param>
    /// <returns>资格参考详情。</returns>
    [HttpGet("api/booking/staff/v1/qualification-references/{qualificationId}", Name = "getQualificationReference")]
    [ProducesResponseType(typeof(ApiEnvelope<QualificationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsRead)]
    public async Task<IResult> GetAsync(string qualificationId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(qualificationId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, QualificationMapper.ToView);
    }

    /// <summary>
    /// CQ05 分页查询资格审计。
    /// </summary>
    /// <returns>分页的资格审计列表；分页信息在 meta.pagination。</returns>
    [HttpGet("api/booking/staff/v1/qualification-audits", Name = "listQualificationAudits")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<QualificationAuditView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsRead)]
    public async Task<IResult> ListAuditsAsync([FromQuery] QualificationAuditListRequest request, CancellationToken cancellationToken)
    {
        var query = QualificationMapper.ToAuditListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAuditsAsync(query, cancellationToken), query.List, QualificationMapper.ToAuditView);
    }

    /// <summary>
    /// CQ03 升级资格例外。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <param name="qualificationId">资格参考标识。</param>
    /// <returns>升级后的资格参考。</returns>
    [HttpPost("api/booking/staff/v1/qualification-references/{qualificationId}/escalations", Name = "escalateQualification")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(QualificationJustificationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QualificationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsEscalate)]
    public Task<IResult> EscalateAsync(string qualificationId, CancellationToken cancellationToken) =>
        ExecuteAsync(qualificationId, escalateHandler.HandleAsync, cancellationToken);

    /// <summary>
    /// CQ04 批准资格例外。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。
    /// </remarks>
    /// <param name="qualificationId">资格参考标识。</param>
    /// <returns>批准后的资格参考。</returns>
    [HttpPost("api/booking/staff/v1/qualification-references/{qualificationId}/exception-approvals", Name = "approveQualificationException")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(QualificationJustificationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QualificationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsApprove)]
    public Task<IResult> ApproveAsync(string qualificationId, CancellationToken cancellationToken) =>
        ExecuteAsync(qualificationId, approveHandler.HandleAsync, cancellationToken);

    /// <summary>
    /// 拒绝资格例外（经理）。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；仅 ESCALATED 可拒绝，拒绝后状态为 REJECTED。拒绝只记录复审结果，不修改外部会员、积分或黑名单；申请人会收到站内通知。X-Expected-Version 可选且目前不参与比较。
    /// </remarks>
    /// <param name="qualificationId">资格参考标识。</param>
    /// <returns>拒绝后的资格参考。</returns>
    [HttpPost("api/booking/staff/v1/qualification-references/{qualificationId}/exception-rejections", Name = "rejectQualificationException")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(QualificationJustificationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QualificationView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.QualificationsApprove)]
    public Task<IResult> RejectAsync(string qualificationId, CancellationToken cancellationToken) =>
        ExecuteAsync(qualificationId, rejectHandler.HandleAsync, cancellationToken);

    /// <summary>
    /// 发起经理复审申请（原型的 REQUEST MANAGER REVIEW / NEW REQUEST）。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。同宾客 + 物业已有未决申请时不重复创建：已是 ESCALATED 的原样返回（200），仅待复核的升级为 ESCALATED（200），否则新建一条 ESCALATED 申请（201）。
    /// 申请人、角色和申请时间来自令牌与服务端时钟，进入 request 快照，之后的批准 / 拒绝不会覆盖。同一宾客 + 物业的并发首次申请没有唯一约束，极端并发下可能各建一条。
    /// </remarks>
    /// <returns>申请对应的资格参考。</returns>
    [HttpPost("api/booking/staff/v1/qualification-references", Name = "createQualificationRequest")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateQualificationRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<QualificationView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.QualificationsEscalate)]
    public async Task<IResult> CreateRequestAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateQualificationRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = QualificationMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), QualificationMapper.ToView);
    }

    // 升级与批准的请求形状相同：说明必填，期望版本与幂等键必填。
    private async Task<IResult> ExecuteAsync(
        string qualificationId,
        Func<QualificationExceptionCommand, CancellationToken, Task<OperationResult<QualificationReference>>> handle,
        CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<QualificationJustificationRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, version, key) is { } failure)
        {
            return failure;
        }

        var command = QualificationMapper.ToCommand(qualificationId, version.Value, body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(await handle(command.Value, cancellationToken), QualificationMapper.ToView);
    }
}
