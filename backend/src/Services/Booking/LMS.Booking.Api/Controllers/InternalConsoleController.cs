using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Security;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Application.Guests.Ports;
using LMS.Booking.Application.Trips.Commands;
using LMS.Booking.Application.Trips.Handlers;
using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 服务间接口：行程状态推进（一期运维手工 / 二期司机 App）与宾客读模型导入（一期运维）。
/// 不经网关；转发终端用户令牌并按其权限与物业范围校验。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1")]
public sealed class InternalConsoleController(
    IAdvanceTripStatusHandler advanceHandler,
    IGuestDirectory guestDirectory) : ControllerBase
{
    private const int MaxGuests = 500;

    /// <summary>
    /// 推进行程状态；非法转移（含终态）返回 409 / 40900。
    /// </summary>
    [HttpPatch("trips/{tripId}/status")]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> AdvanceTripStatusAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<AdvanceStatusRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        if (!LMS.BuildingBlocks.Core.Wire.WireEnum.TryParse<TripStatus>(body.Value.Status, out var target))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("status", "Unknown trip status")]);
        }

        var command = new AdvanceTripStatusCommand(tripId, target, CallerContexts.From(HttpContext));
        var result = await advanceHandler.HandleAsync(command, cancellationToken);

        return OperationResultMapper.ToUpdatedResult(result, _ => new { });
    }

    /// <summary>
    /// 批量导入/刷新宾客快照（按会员号幂等 upsert），返回导入条数。
    /// </summary>
    [HttpPost("guests")]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> ImportGuestsAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<ImportGuestsRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var guests = body.Value.Guests;
        if (guests is null || guests.Count == 0 || guests.Count > MaxGuests)
        {
            return ApiResults.ValidationFailed([new ApiFieldError("guests", $"Must contain 1 to {MaxGuests} guests")]);
        }

        if (guests.Any(guest => string.IsNullOrWhiteSpace(guest.MemberId) || string.IsNullOrWhiteSpace(guest.Name) || string.IsNullOrWhiteSpace(guest.TierCode)))
        {
            return ApiResults.ValidationFailed([new ApiFieldError("guests", "memberId, name and tierCode are required for every guest")]);
        }

        foreach (var guest in guests)
        {
            await guestDirectory.UpsertAsync(ToDomain(guest), cancellationToken);
        }

        return ApiResults.Ok(new { imported = guests.Count });
    }

    private static GuestProfile ToDomain(ImportGuestRequest guest) => new(
        string.IsNullOrWhiteSpace(guest.GuestId) ? "guest_" + Guid.CreateVersion7().ToString("N") : guest.GuestId!,
        guest.MemberId!,
        guest.Name!,
        guest.TierCode!,
        guest.RoomNumber,
        guest.PropertyId,
        ParseDate(guest.StayFrom),
        ParseDate(guest.StayTo),
        guest.PointsBalance,
        NormalizePhone(guest.Phone),
        guest.InHouse,
        guest.PlayVerified ?? true,
        guest.PlayWindowDays ?? 90,
        guest.PreferredLanguage,
        guest.PreferredVehicleClass,
        guest.ConfirmationChannel,
        guest.SpecialRequests);

    // 宾客电话同时是客户手机号密码登录的标识：能规范化成含国家/地区码的完整号码时按统一规范保存；
    // 无法规范化的号码原样保存（快照导入不拒绝），该宾客无法用手机号登录，需由数据维护修正。
    private static string? NormalizePhone(string? phone) =>
        PhoneNumber.TryNormalize(phone, out var normalized) ? normalized : phone;

    private static DateOnly? ParseDate(string? text) =>
        text is null ? null : DateOnly.ParseExact(text, "yyyy-MM-dd");

    private sealed record AdvanceStatusRequest(string? Status);

    private sealed record ImportGuestsRequest(IReadOnlyList<ImportGuestRequest>? Guests);

    private sealed record ImportGuestRequest(
        string? GuestId,
        string? MemberId,
        string? Name,
        string? TierCode,
        string? RoomNumber,
        string? PropertyId,
        string? StayFrom,
        string? StayTo,
        long? PointsBalance,
        string? Phone,
        bool InHouse,
        bool? PlayVerified,
        int? PlayWindowDays,
        string? PreferredLanguage,
        string? PreferredVehicleClass,
        string? ConfirmationChannel,
        string? SpecialRequests);
}
