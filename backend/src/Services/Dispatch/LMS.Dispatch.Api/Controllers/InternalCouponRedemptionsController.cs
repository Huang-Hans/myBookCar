using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Internal;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Handlers;
using LMS.Dispatch.Application.Coupons.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

// Booking 在優惠券訂單建立與取消時調用。只接受 Booking 的服務令牌。
[ApiController]
[Authorize(Policy = AuthorizationPolicies.CouponRedemptionService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/coupon-redemptions")]
public sealed class InternalCouponRedemptionsController(ICouponRedemptionHandler redemptions) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<CouponRedemptionView>), StatusCodes.Status200OK)]
    public async Task<IResult> RedeemAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<RedeemCouponRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var command = new RedeemCouponCommand(
            body.Value.RedemptionKey ?? string.Empty,
            body.Value.CouponId ?? string.Empty,
            body.Value.MemberId,
            body.Value.GuestId,
            body.Value.PropertyId,
            body.Value.VehicleClass);
        return ToResult(await redemptions.RedeemAsync(command, cancellationToken));
    }

    [HttpPost("{redemptionKey}/restoration")]
    [ProducesResponseType(typeof(ApiEnvelope<CouponRedemptionView>), StatusCodes.Status200OK)]
    public async Task<IResult> RestoreAsync(string redemptionKey, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        return ToResult(await redemptions.RestoreAsync(redemptionKey, cancellationToken));
    }

    private static IResult ToResult(LMS.BuildingBlocks.Core.Operations.OperationResult<CouponRedemptionResult> result) =>
        OperationResultMapper.ToUpdatedResult(
            result,
            value => new CouponRedemptionView(value.RedemptionKey, value.CouponId, value.RedeemedCount, value.Restored));
}
