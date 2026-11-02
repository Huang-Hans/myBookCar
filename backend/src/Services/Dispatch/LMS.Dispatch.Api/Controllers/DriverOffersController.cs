using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Offers;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Offers;
using LMS.Dispatch.Application.Offers.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 司机本人的派单邀请接口。邀请只对被邀请的司机本人可见，他人的邀请与不存在不作区分。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Driver)]
[Tags("司机邀请")]
[Route("api/dispatch/driver/v1/me/offers")]
public sealed class DriverOffersController(
    IDriverOfferQueryHandler queryHandler,
    IRespondToOfferHandler respondHandler,
    TimeProvider timeProvider) : ControllerBase
{
    /// <summary>
    /// D03 查询本人有效邀请。
    /// </summary>
    /// <remarks>
    /// 只返回待响应且未过期，或处理中（ACCEPTING）的邀请；每项带 serverTime 与 expiresAt 用于校准倒计时。响应不缓存。
    /// </remarks>
    /// <returns>分页的邀请列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listDriverOffers")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<DriverOfferView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverSelfRead)]
    public async Task<IResult> ListAsync([FromQuery] DriverOfferListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var serverTime = timeProvider.GetUtcNow();
        var result = await queryHandler.ListAsync(HttpContext.GetPrincipal().EntityId, list, cancellationToken);

        return PagedResponses.ToResult(result, list, offer => DriverOfferMapper.ToView(offer, serverTime));
    }

    /// <summary>
    /// D04 接受或拒绝邀请。
    /// </summary>
    /// <remarks>
    /// decision 为 ACCEPT 或 DECLINE。重复提交同一决定返回同一结果；相反的决定、已过期或已被取代的邀请返回 409。
    /// ACCEPT 只有在 Booking 权威指派成功后才返回 200 与 ACCEPTED；Booking 暂时不可用时返回 202 与 ACCEPTING，用同一邀请重试即可，不会产生第二笔指派。
    /// </remarks>
    /// <param name="offerId">邀请标识。</param>
    /// <returns>响应后的邀请。</returns>
    [HttpPost("{offerId}/responses", Name = "respondToDriverOffer")]
    [ApiRequestBody(typeof(OfferResponseRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<DriverOfferView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<DriverOfferView>), StatusCodes.Status202Accepted)]
    [RequirePrincipal(PrincipalType.Driver, PrincipalPermissionCodes.DriverTripsWrite)]
    public async Task<IResult> RespondAsync(string offerId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<OfferResponseRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var command = DriverOfferMapper.ToRespondCommand(offerId, HttpContext.GetPrincipal().EntityId, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        var result = await respondHandler.HandleAsync(command.Value, cancellationToken);

        return result.IsSuccess ? ToSuccess(result.Value!) : OperationResultMapper.ToFailure(result);
    }

    private IResult ToSuccess(LMS.Dispatch.Application.Offers.Models.OfferResponse response)
    {
        var view = DriverOfferMapper.ToView(response.Offer, timeProvider.GetUtcNow());

        return response.IsProcessing ? ApiResults.Accepted(view) : ApiResults.Ok(view);
    }
}
