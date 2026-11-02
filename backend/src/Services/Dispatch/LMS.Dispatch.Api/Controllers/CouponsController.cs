using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Api.Contracts.Coupons;
using LMS.Dispatch.Api.Controllers.Support;
using LMS.Dispatch.Api.Mapping.Coupons;
using LMS.Dispatch.Application.Coupons.Commands;
using LMS.Dispatch.Application.Coupons.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Dispatch.Api.Controllers;

/// <summary>
/// 優惠券管理。發放免費行程、車資抵扣與會員禮遇，每張券綁定一名會員。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("優惠券")]
[Route("api/dispatch/staff/v1/coupons")]
public sealed class CouponsController(
    ICouponQueryHandler queryHandler,
    ICreateCouponHandler createHandler,
    IUpdateCouponHandler updateHandler,
    IRemoveCouponHandler removeHandler) : ControllerBase
{
    /// <summary>
    /// CP01 分頁查詢優惠券。
    /// </summary>
    /// <remarks>
    /// 搜尋券碼或名稱，可與會員編號、類型、狀態同時篩選。狀態按澳門業務日推導。
    /// </remarks>
    /// <returns>分頁的優惠券列表；分頁資訊在 meta.pagination。</returns>
    [HttpGet(Name = "listCoupons")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CouponView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> ListAsync([FromQuery] CouponListRequest request, CancellationToken cancellationToken)
    {
        var query = CouponMapper.ToListQuery(request, HttpContext.GetCaller().Scope);

        return PagedResponses.ToResult(await queryHandler.ListAsync(query, cancellationToken), query.List, CouponMapper.ToView);
    }

    /// <summary>
    /// CP02 統計生效中、發行量、已使用次數與即將生效。
    /// </summary>
    /// <remarks>
    /// 對應優惠券管理頂部四張卡片。只按呼叫者可見範圍統計。
    /// </remarks>
    /// <returns>四項合計。沒有優惠券時各為 0。</returns>
    [HttpGet("summary", Name = "summarizeCoupons")]
    [ProducesResponseType(typeof(ApiEnvelope<CouponSummaryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> SummarizeAsync(CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.SummarizeAsync(HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, CouponMapper.ToSummary);
    }

    /// <summary>
    /// CP03 查詢優惠券詳情。
    /// </summary>
    /// <param name="couponId">優惠券標識。</param>
    /// <returns>優惠券詳情，含綁定的會員編號。不在呼叫者可見範圍時按不可見處理。</returns>
    [HttpGet("{couponId}", Name = "getCoupon")]
    [ProducesResponseType(typeof(ApiEnvelope<CouponView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchRead)]
    public async Task<IResult> GetAsync(string couponId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(couponId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, CouponMapper.ToView);
    }

    /// <summary>
    /// CP04 新增優惠券。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。memberId 必填，券只屬於該會員。券碼不區分大小寫唯一。
    /// 全物業與限定車型只允許全物業授權建立。
    /// </remarks>
    /// <returns>新建的優惠券。</returns>
    [HttpPost(Name = "createCoupon")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateCouponRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CouponView>), StatusCodes.Status201Created)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateCouponRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = CouponMapper.ToCreateCommand(body.Value, key.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await createHandler.HandleAsync(command.Value, cancellationToken), CouponMapper.ToView);
    }

    /// <summary>
    /// CP05 部分更新優惠券。
    /// </summary>
    /// <remarks>
    /// 至少傳一個欄位。memberId 傳入後改綁該會員。改成全物業或限定車型只允許全物業授權。
    /// 發行量不能小於已使用次數。
    /// </remarks>
    /// <param name="couponId">優惠券標識。</param>
    /// <returns>更新後的優惠券。不在呼叫者可見範圍時按不可見處理。</returns>
    [HttpPatch("{couponId}", Name = "updateCoupon")]
    [ApiRequestBody(typeof(PatchCouponRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CouponView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> UpdateAsync(string couponId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<PatchCouponRequest>(HttpContext, cancellationToken);
        var version = RequestHeaders.ReadExpectedVersion(HttpContext);
        if (RequestParse.FirstFailure(body, version) is { } failure)
        {
            return failure;
        }

        var command = CouponMapper.ToUpdateCommand(couponId, version.Value, body.Value, CallerContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(await updateHandler.HandleAsync(command.Value, cancellationToken), CouponMapper.ToView);
    }

    /// <summary>
    /// CP06 刪除優惠券。
    /// </summary>
    /// <remarks>
    /// 刪除後保留審計。不存在或不可見時返回 404。全物業與限定車型只允許全物業授權刪除。
    /// </remarks>
    /// <param name="couponId">優惠券標識。</param>
    /// <returns>成功時不帶資料。</returns>
    [HttpDelete("{couponId}", Name = "removeCoupon")]
    [ProducesResponseType(typeof(ApiEnvelope<object>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.DispatchWrite)]
    public async Task<IResult> RemoveAsync(string couponId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await removeHandler.HandleAsync(new RemoveCouponCommand(couponId, CallerContexts.From(HttpContext)), cancellationToken);

        return result.IsSuccess ? ApiResults.Ok<object?>(null) : OperationResultMapper.ToFailure(result);
    }
}
