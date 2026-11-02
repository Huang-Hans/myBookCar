using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Application.Trips.Handlers;
using LMS.Booking.Application.Trips.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 行程状态计数（HA-04 状态 chips）。键为状态字面值，8 态恒全量返回。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服行程")]
public sealed class TripStatusSummariesController(ITripQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// 在共同筛选条件下统计各状态的行程数。
    /// </summary>
    /// <remarks>
    /// 查询参数：search、propertyId、from、to、redemptionState，含义与行程列表完全一致，但不含 status：页签数字要在其余筛选下给出每个状态的数量。
    /// 不带任何参数时为调用者物业范围内的全局计数。
    /// </remarks>
    [HttpGet("api/booking/staff/v1/trip-status-summaries", Name = "getTripStatusSummaries")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyDictionary<string, long>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsRead)]
    public async Task<IResult> GetAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var search = reader.ReadSearch();
        var propertyId = reader.OptionalText("propertyId");
        var from = reader.OptionalUtcInstant("from");
        var to = reader.OptionalUtcInstant("to");
        var redemptionState = reader.OptionalText("redemptionState");
        if (from is not null && to is not null && to <= from)
        {
            reader.Reject("to", "Must be later than from");
        }

        if (reader.Failure() is { } failure)
        {
            return failure;
        }

        var query = new TripSummaryQuery(HttpContext.GetCaller().Scope, propertyId, from, to, redemptionState, search);
        var result = await queryHandler.SummariesAsync(query, cancellationToken);
        if (!result.IsSuccess)
        {
            return OperationResultMapper.ToFailure(result);
        }

        var counts = result.Value!.ToDictionary(pair => WireEnum.ToWire(pair.Key), pair => pair.Value);

        return ApiResults.Ok(counts);
    }
}
