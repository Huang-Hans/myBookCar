using LMS.Booking.Application.SelfService.Options;
using Microsoft.Extensions.Options;
using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Errors;
using LMS.BuildingBlocks.Http.Documentation;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Api.Contracts.Customer;
using LMS.Booking.Api.Controllers.Support;
using LMS.Booking.Api.Mapping.Customer;
using LMS.Booking.Application.SelfService.Handlers;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.SelfService.Services;
using LMS.Booking.Application.Trips.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Booking.Api.Controllers;

/// <summary>
/// 客户本人的订单接口（自助建单、订单列表与详情、取消）。客户标识只来自令牌绑定；
/// 他人的订单与不存在不作区分，一律 404。再次预订复用建单，不提供复制订单即确认的接口。
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Customer)]
[Tags("客户订单")]
[Route("api/booking/customer/v1/me/trips")]
public sealed class CustomerTripsController(
    ICreateSelfServiceTripHandler createHandler,
    ICustomerTripQueryHandler queryHandler,
    IDriverContactDirectory driverContacts,
    ICustomerCancellationHandler cancellationHandler,
    SelfCancellationRules cancellationRules, IOptions<SelfServiceOptions> products) : ControllerBase
{
    /// <summary>
    /// C05 客户自助建单。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key；同键同内容重放同样 201。客户由令牌绑定，资格、可订服务、时间窗、路线、车型容量与物业配额都在提交时重新计算。
    /// 候补或跨境待审批的订单受理为 PENDING，不会显示为已确认；返回的订单号与状态是持久化的权威结果。
    /// </remarks>
    /// <returns>新建的订单。</returns>
    [HttpPost(Name = "createCustomerTrip")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CreateCustomerTripRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerTripView>), StatusCodes.Status201Created)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsWrite)]
    public async Task<IResult> CreateAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CreateCustomerTripRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var guestId = HttpContext.GetPrincipal().EntityId;
        var command = CustomerRequestMapper.ToCreateCommand(guestId, body.Value, key.Value, PrincipalContexts.From(HttpContext), products.Value);
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToResult(
            await createHandler.HandleAsync(command.Value, cancellationToken), trip => CustomerTripMapper.ToDetail(trip, cancellationRules));
    }

    /// <summary>
    /// C06 分页查询本人订单。
    /// </summary>
    /// <remarks>
    /// group=ACTIVE 为全部非终态订单（不只等于后端 IN_PROGRESS），COMPLETED / CANCELLED 为终态；按下单时间倒序，稳定分页。
    /// 订单归属按实际受益宾客判定，包含客服代订给本人的订单。空结果为真实空列表，查询失败返回错误。响应不缓存。
    /// </remarks>
    /// <returns>分页的订单列表；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listCustomerTrips")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<CustomerTripSummaryView>>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> ListAsync([FromQuery] CustomerTripListRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var list = request.ToListQuery();
        var query = new ListGuestTripsQuery(list, HttpContext.GetPrincipal().EntityId, request.Group);

        return PagedResponses.ToResult(
            await queryHandler.ListAsync(query, cancellationToken), list, trip => CustomerTripMapper.ToSummary(trip, cancellationRules));
    }

    /// <summary>
    /// C07 查询本人订单详情。
    /// </summary>
    /// <remarks>
    /// 返回订单摘要、备注、状态轨迹、司机与车辆摘要（未分配时省略并给出 UNASSIGNED）、积分引用和取消信息；终态订单附再次预订输入。响应不缓存。
    /// </remarks>
    /// <param name="tripId">订单（行程）标识。</param>
    /// <returns>订单详情。</returns>
    [HttpGet("{tripId}", Name = "getCustomerTrip")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerTripView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, trip => CustomerTripMapper.ToDetail(trip, cancellationRules));
    }

    /// <summary>Get the assigned driver's phone for an active trip owned by the caller.</summary>
    [HttpGet("{tripId}/driver-contacts", Name = "getCustomerTripDriverContact")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerDriverContactView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetDriverContactAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await queryHandler.GetAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);
        if (!result.IsSuccess)
        {
            return OperationResultMapper.ToFailure(result);
        }

        var trip = result.Value!;
        if (!CustomerDriverContactRules.CanContact(trip))
        {
            return ApiResults.Fail(ApiErrors.ResourceNotFound);
        }

        var contact = await driverContacts.GetPhoneNumberAsync(trip.Assignment!.DriverId, cancellationToken);
        return contact.Outcome switch
        {
            DirectoryOutcome.Success when !string.IsNullOrWhiteSpace(contact.Value) => ApiResults.Ok(new CustomerDriverContactView(contact.Value)),
            DirectoryOutcome.NotFound => ApiResults.Fail(ApiErrors.ResourceNotFound),
            _ => ApiResults.Fail(ApiErrors.ServiceUnavailable)
        };
    }

    /// <summary>
    /// C08 查询本人订单的取消能力与政策。
    /// </summary>
    /// <param name="tripId">订单（行程）标识。</param>
    /// <returns>取消能力、不可取消原因与物业取消政策引用。</returns>
    [HttpGet("{tripId}/cancellation-policy", Name = "getCustomerTripCancellationPolicy")]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerCancellationPolicyView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsRead)]
    public async Task<IResult> GetCancellationPolicyAsync(string tripId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        Response.Headers.CacheControl = "no-store";
        var result = await cancellationHandler.GetPolicyAsync(tripId, HttpContext.GetPrincipal().EntityId, cancellationToken);

        return OperationResultMapper.ToResult(result, CustomerTripMapper.ToPolicy);
    }

    /// <summary>
    /// C09 取消本人订单。
    /// </summary>
    /// <remarks>
    /// 需要 Idempotency-Key。进行中与终态订单不可取消（409）。取消原因由服务端配置映射，客户端只可附备注。
    /// 已指派司机的订单取消后，该订单进入司机的已取消历史。
    /// </remarks>
    /// <param name="tripId">订单（行程）标识。</param>
    /// <returns>取消后的订单。</returns>
    [HttpPost("{tripId}/cancellations", Name = "createCustomerTripCancellation")]
    [IdempotencyKeyHeader]
    [ApiRequestBody(typeof(CancelCustomerTripRequest))]
    [ProducesResponseType(typeof(ApiEnvelope<CustomerTripView>), StatusCodes.Status200OK)]
    [RequirePrincipal(PrincipalType.Customer, PrincipalPermissionCodes.CustomerBookingsWrite)]
    public async Task<IResult> CancelAsync(string tripId, CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<CancelCustomerTripRequest>(HttpContext, cancellationToken);
        var key = RequestHeaders.ReadIdempotencyKey(HttpContext);
        if (RequestParse.FirstFailure(body, key) is { } failure)
        {
            return failure;
        }

        var command = CustomerRequestMapper.ToCancelCommand(
            tripId, HttpContext.GetPrincipal().EntityId, body.Value, key.Value, PrincipalContexts.From(HttpContext));
        if (command.Failure is not null)
        {
            return command.Failure;
        }

        return OperationResultMapper.ToUpdatedResult(
            await cancellationHandler.CancelAsync(command.Value, cancellationToken), trip => CustomerTripMapper.ToDetail(trip, cancellationRules));
    }
}
