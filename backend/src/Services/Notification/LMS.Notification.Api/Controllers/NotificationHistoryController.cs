using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Http.Contracts;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Results;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Notification.Api.Contracts.History;
using LMS.Notification.Api.Mapping.History;
using LMS.Notification.Application.History.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Notification.Api.Controllers;

/// <summary>
/// 通知投递历史接口。
/// </summary>
/// <remarks>
/// 只读：没有发送、重发或模板编辑入口。投递事实由外部来源写入；来源未接入时读取返回 503 / 50300，
/// 不返回空列表冒充真实结果。通知记录没有物业归属，目前只有全物业调用者可以读取。
/// </remarks>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.Staff)]
[Tags("通知历史")]
[Route("api/notification/staff/v1/notification-history")]
public sealed class NotificationHistoryController(INotificationHistoryQueryHandler queryHandler) : ControllerBase
{
    /// <summary>
    /// NT01 分页查询通知投递历史。
    /// </summary>
    /// <returns>分页的投递历史；分页信息在 meta.pagination。</returns>
    [HttpGet(Name = "listNotificationHistory")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<NotificationHistoryView>>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> ListAsync([FromQuery] NotificationHistoryListRequest request, CancellationToken cancellationToken)
    {
        var query = NotificationHistoryMapper.ToListQuery(request);
        var result = await queryHandler.ListAsync(query, HttpContext.GetCaller().Scope, cancellationToken);

        return PagedResponses.ToResult(result, query.List, NotificationHistoryMapper.ToView);
    }

    /// <summary>
    /// NT02 读取通知内容预览。
    /// </summary>
    /// <remarks>
    /// 读取已记录的预览文本，不触发任何发送。
    /// </remarks>
    /// <param name="notificationId">通知记录标识。</param>
    /// <returns>通知记录；不存在时返回 404。</returns>
    [HttpGet("{notificationId}", Name = "getNotificationHistory")]
    [ProducesResponseType(typeof(ApiEnvelope<NotificationHistoryView>), StatusCodes.Status200OK)]
    [RequirePermission(PermissionCodes.NotificationsRead)]
    public async Task<IResult> GetAsync(string notificationId, CancellationToken cancellationToken)
    {
        if (NoBodyRequest.Validate(HttpContext) is { } failure)
        {
            return failure;
        }

        var result = await queryHandler.GetAsync(notificationId, HttpContext.GetCaller().Scope, cancellationToken);

        return OperationResultMapper.ToResult(result, NotificationHistoryMapper.ToView);
    }
}
