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
using LMS.Dispatch.Application.Shifts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("排班")]
[Route("api/dispatch/staff/v1/shifts/withdrawals")]
public sealed class ShiftWithdrawalsController(IWithdrawShiftsHandler handler) : ControllerBase
{
    /// <summary>Withdraw one driver shift or the confirmed period's shifts atomically, retaining history.</summary>
    /// <remarks>Requires dispatch:write. Targets contain the displayed IDs and versions. A changed period or version returns 409.
    /// Repeating the same targets is a no-op for withdrawn records and never withdraws replacement shifts.</remarks>
    [HttpPost(Name = "withdrawShifts")]
    [ApiRequestBody(typeof(WithdrawShiftsRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<ShiftWithdrawalView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> WithdrawAsync(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var body = await JsonRequestBody.ReadAsync<WithdrawShiftsRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null) return body.Failure;
        var command = ShiftWithdrawalMapper.ToCommand(body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null) return command.Failure;
        var result = await handler.HandleAsync(command.Value, cancellationToken);
        return ToResult(result);
    }

    private static IResult ToResult(OperationResult<ShiftWithdrawalResult> result) => result.Outcome switch
    {
        OperationOutcome.SourceUnavailable => ApiResults.Fail(ApiErrors.ServiceUnavailable, ToErrors(result)),
        OperationOutcome.StateConflict => ApiResults.Fail(ApiErrors.StateConflict, ToErrors(result)),
        _ => OperationResultMapper.ToResult(result, ShiftWithdrawalMapper.ToView)
    };

    private static List<ApiFieldError> ToErrors(OperationResult<ShiftWithdrawalResult> result) =>
        result.Issues.Select(issue => new ApiFieldError(issue.Field, issue.Message)).ToList();
}
