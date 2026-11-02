using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Platform.Api.Contracts.Internal;
using LMS.Platform.Application.Notifications.Handlers;
using LMS.Platform.Application.Notifications.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Platform.Api.Controllers;

/// <summary>
/// 服务间接口：Booking 后台解析某物业内有资格接收客户下单通知的客服（含离线客服）。
/// </summary>
/// <remarks>
/// 不经网关。调用者是 Booking 的受限服务身份（ServiceBearer，仅含 notification-recipients:query 范围），
/// 不接受员工、司机或客户令牌，也不继承任何员工角色。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.NotificationRecipientService)]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("internal/v1/notification-recipients/queries")]
public sealed class InternalNotificationRecipientsController(INotificationRecipientQueryHandler handler) : ControllerBase
{
    /// <summary>
    /// 查询收件人。成功时 data.recipients 为数组（可为空）；收件人角色集合未配置或目录暂不可用返回 503。
    /// </summary>
    [HttpPost]
    public async Task<IResult> QueryAsync(CancellationToken cancellationToken)
    {
        var body = await JsonRequestBody.ReadAsync<NotificationRecipientsQueryRequest>(HttpContext, cancellationToken);
        if (body.Failure is not null)
        {
            return body.Failure;
        }

        var errors = Validate(body.Value);
        if (errors.Count > 0)
        {
            return ApiResults.ValidationFailed(errors);
        }

        var result = await handler.HandleAsync(new NotificationRecipientQuery(body.Value.EventType!, body.Value.PropertyId!.Trim()), cancellationToken);

        return result.IsSuccess
            ? ApiResults.Ok(new NotificationRecipientsView(result.Value!.Select(userId => new NotificationRecipientView(userId)).ToList()))
            : OperationResultMapper.ToFailure(result);
    }

    private static List<ApiFieldError> Validate(NotificationRecipientsQueryRequest request)
    {
        var errors = new List<ApiFieldError>();
        if (request.EventType != NotificationRecipientQuery.BookingCreated)
        {
            errors.Add(new ApiFieldError("eventType", $"Must be {NotificationRecipientQuery.BookingCreated}"));
        }

        if (string.IsNullOrWhiteSpace(request.PropertyId))
        {
            errors.Add(new ApiFieldError("propertyId", "Is required"));
        }

        return errors;
    }
}
