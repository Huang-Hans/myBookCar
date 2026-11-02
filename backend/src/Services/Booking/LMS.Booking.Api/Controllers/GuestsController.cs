using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Guests;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Guests;
using LMS.Booking.Application.Guests.Handlers;
using LMS.Booking.Application.Guests.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 宾客检索、档案与资格判定接口（HA-01 / HA-02 / HA-06）。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服宾客")]
public sealed class GuestsController(IGuestQueryHandler queryHandler, IUpdateGuestHandler updateHandler) : ControllerBase
{
    private static readonly ListQuerySpec ListSpec =
        new(GuestSorting.Fields, GuestSorting.DefaultSort, GuestSorting.StableField);

    /// <summary>Update member details within the caller's property scope.</summary>
    [HttpPatch("api/booking/staff/v1/guests/{guestId}", Name = "updateGuest")]
    [ApiRequestBody(typeof(UpdateGuestRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<GuestSummaryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.GuestsWrite)]
    public async Task<IResult> UpdateAsync(string guestId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<UpdateGuestRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
            return body.Failure;
        var command = GuestRequestMapper.ToCommand(guestId, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
            return command.Failure;
        return OperationResultMapper.ToUpdatedResult(
            await updateHandler.UpdateAsync(command.Value, cancellationToken), GuestMapper.ToSummaryView);
    }

    /// <summary>
    /// 按姓名/会员号/房号/电话检索宾客。
    /// </summary>
    /// <remarks>
    /// 查询参数：page、pageSize、sort、search，以及 tier。
    /// </remarks>
    [HttpGet("api/booking/staff/v1/guests", Name = "listGuests")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<GuestSummaryView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.GuestsRead)]
    public async Task<IResult> ListAsync(CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        var list = reader.ReadList(ListSpec);
        var tier = reader.OptionalText("tier");
        if (reader.Failure() is { } failure)
        {
            return failure;
        }

        var query = new ListGuestsQuery(list, HttpContext.GetCaller().Scope, tier);

        return PagedResponses.ToResult(await queryHandler.SearchAsync(query, cancellationToken), list, GuestMapper.ToSummaryView);
    }

    /// <summary>
    /// 宾客档案：会员信息、黑名单状态（实时读 Platform）、偏好与最近行程。
    /// </summary>
    [HttpGet("api/booking/staff/v1/guests/{guestId}", Name = "getGuestProfile")]
    [ProducesResponseType(typeof(ApiEnvelope<GuestProfileView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.GuestsRead)]
    public async Task<IResult> GetAsync(string guestId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetProfileAsync(guestId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, GuestMapper.ToProfileView);
    }

    /// <summary>
    /// 资格判定（HA-02）：等级门槛、游玩核验、可用车型与积分。不通过时登记待经理复核档案。
    /// </summary>
    [HttpGet("api/booking/staff/v1/guests/{guestId}/qualification-check", Name = "checkGuestQualification")]
    [ProducesResponseType(typeof(ApiEnvelope<GuestQualificationCheckView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.GuestsRead)]
    public async Task<IResult> CheckQualificationAsync(string guestId, CancellationToken cancellationToken)
    {
        var reader = QueryReader.For(HttpContext);
        _ = reader.ReadSearch();
        var propertyId = reader.OptionalText("propertyId");
        if (reader.Failure() is { } failure)
        {
            return failure;
        }

        if (propertyId is null)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("propertyId", "Required")]);
        }

        var result = await queryHandler.CheckQualificationAsync(
            guestId, propertyId, HttpContext.GetCaller().Scope, CallerContexts.From(HttpContext), cancellationToken);

        return OperationResultMapper.ToResult(result, GuestMapper.ToView);
    }
}
