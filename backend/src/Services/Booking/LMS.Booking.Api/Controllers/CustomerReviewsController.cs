using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.CustomerRecords;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.CustomerRecords;
using LMS.Booking.Application.Reviews.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客户行程评价接口（评分与评语）。只有本人已完成的行程可以评价；他人的行程与不存在不作区分，一律 404。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[Tags("客户评价")]
[Route("api/booking/customer/v1/me")]
public sealed class CustomerReviewsController(
    IReviewableTripsQueryHandler reviewableHandler,
    ITripReviewHandler reviewHandler) : ControllerBase
{
    /// <summary>
    /// C2-07 分页查询可评价的本人已完成行程及评价摘要。
    /// </summary>
    /// <remarks>
    /// 服务端分页，默认按结束时间倒序。没有评价的行程不带 review。评价窗口待业务确认，目前不设期限。响应不缓存。
    /// </remarks>
    /// <returns>分页的可评价行程；分页信息在 meta.pagination。</returns>
    [HttpGet("reviewable-trips", Name = "listCustomerReviewableTrips")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<ReviewableTripView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerReviewsRead)]
    public async Task<IResult> ListReviewableAsync([FromQuery] ReviewableTripListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var result = await reviewableHandler.ListAsync(HttpContext.GetPrincipal().EntityId, list, cancellationToken);

        return PagedResponses.ToResult(result, list, CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-08 查询本人行程的评价。
    /// </summary>
    /// <param name="tripId">行程标识。</param>
    /// <returns>评价；行程不是本人的或还没有评价为 404。</returns>
    [HttpGet("trips/{tripId}/review", Name = "getCustomerTripReview")]
    [ProducesResponseType(typeof(ApiEnvelope<TripReviewView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerReviewsRead)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await reviewHandler.GetAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerRecordMapper.ToView);
    }

    /// <summary>
    /// C2-09 创建或修改本人行程的评价。
    /// </summary>
    /// <remarks>
    /// 完整写入 rating 与 comments。首次创建 201；内容与已保存的相同返回已保存的评价；修改已有评价必须带 X-Expected-Version，缺失或不一致为 409。
    /// 行程不是本人的为 404；行程未完成为 422。评价不改变行程状态、积分或司机等级。
    /// </remarks>
    /// <param name="tripId">行程标识。</param>
    /// <returns>保存后的评价。</returns>
    [HttpPut("trips/{tripId}/review", Name = "upsertCustomerTripReview")]
    [ApiRequestBody(typeof(UpsertTripReviewRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<TripReviewView>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiEnvelope<TripReviewView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerReviewsWrite)]
    public async Task<IResult> UpsertAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<UpsertTripReviewRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = CustomerRecordMapper.ToCommand(
            tripId, HttpContext.GetPrincipal().EntityId, version.Value, body.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        var result = await reviewHandler.UpsertAsync(command.Value, cancellationToken);
        if (!result.IsSuccess)
        {
            return OperationResultMapper.ToFailure(result);
        }

        var view = CustomerRecordMapper.ToView(result.Value!);

        return result.Value!.Version == 1 ? ApiResults.Created(view) : ApiResults.Ok(view);
    }
}
