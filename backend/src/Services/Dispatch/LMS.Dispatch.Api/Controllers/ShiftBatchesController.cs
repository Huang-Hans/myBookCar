using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Shifts;
using LMS.Dispatch.Application.Shifts.Handlers;
using LMS.Dispatch.Domain.Shifts.Batches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("排班")]
[Route("api/dispatch/staff/v1/shifts")]
public sealed class ShiftBatchesController(IShiftBatchPreviewHandler previews, ICreateShiftBatchHandler batches) : ControllerBase
{
    /// <summary>Preview a batch without writing shifts. Existing shifts are retained.</summary>
    [HttpPost("batch-previews", Name = "previewShiftBatch")]
    [ApiRequestBody(typeof(ShiftBatchRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftBatchPreviewView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> PreviewAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var body = await JsonRequestBody.ReadAsync<ShiftBatchRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null) return body.Failure;
        var rules = ShiftBatchMapper.ToRules(body.Value);
        if (rules.Failure is not null) return rules.Failure;
        var result = await previews.HandleAsync(rules.Value, HttpContext.GetCaller().Scope, cancellationToken);
        return OperationResultMapper.ToResult(result, ShiftBatchMapper.ToView);
    }

    /// <summary>Create all new shifts atomically after revalidating the preview. Requires Idempotency-Key.</summary>
    [HttpPost("batches", Name = "createShiftBatch")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateShiftBatchRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftBatchView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var body = await JsonRequestBody.ReadAsync<CreateShiftBatchRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure) return failure;
        var command = ShiftBatchMapper.ToCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null) return command.Failure;
        var result = await batches.HandleAsync(command.Value, cancellationToken);
        return ToBatchResult(result);
    }

    private static IResult ToBatchResult(OperationResult<ShiftBatch> result) => result.Outcome switch
    {
        OperationOutcome.StateConflict => ApiResults.Fail(ApiErrors.StateConflict,
            [new("previewHash", "The preview has changed. Preview the batch again before confirming.")]),
        OperationOutcome.SourceUnavailable when result.Issues.Count > 0 => ApiResults.Fail(ApiErrors.ServiceUnavailable,
            result.Issues.Select(issue => new ApiFieldError(issue.Field, issue.Message)).ToList()),
        _ => OperationResultMapper.ToResult(result, ShiftBatchMapper.ToView)
    };
}
