using LMS.Booking.Application.SelfService.Options;
using Microsoft.Extensions.Options;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Previews;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Previews;
using LMS.Booking.Application.Previews.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 预订预览：在提交前评估一次代订输入，给出是否可提交、候补 / 复审、全部已知的失败原因以及所选车型的积分预览。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("客服行程")]
public sealed class BookingPreviewsController(IBookingPreviewHandler handler, IOptions<SelfServiceOptions> products) : ControllerBase
{
    /// <summary>
    /// 评估一次代订输入。
    /// </summary>
    /// <remarks>
    /// 无副作用：不创建订单、不占配额、不扣积分，也不登记经理复审申请（与资格核对接口不同）。校验链与创建订单完全一致，但创建时仍会重新校验。
    /// 业务规则不满足返回 200 且 canSubmit=false；宾客 / 地点不存在为 404，越权为 403，外部来源不可用为 503。响应不缓存。
    /// </remarks>
    /// <returns>预览结果。</returns>
    [HttpPost("api/booking/staff/v1/booking-previews", Name = "createBookingPreview")]
    [ApiRequestBody(typeof(BookingPreviewRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<BookingPreviewView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.BookingsWrite)]
    public async Task<IResult> PreviewAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<BookingPreviewRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var query = BookingPreviewMapper.ToQuery(body.Value, CallerContexts.From(HttpContext), products.Value);
        if (query.Failure is not null)
        {
            return query.Failure;
        }

        Response.Headers.CacheControl = "no-store";

        return OperationResultMapper.ToUpdatedResult(await handler.HandleAsync(query.Value, cancellationToken), BookingPreviewMapper.ToView);
    }
}
