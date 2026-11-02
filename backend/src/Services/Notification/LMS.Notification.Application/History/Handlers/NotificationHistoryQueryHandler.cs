using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Notification.Application.History.Models;
using LMS.Notification.Application.History.Options;
using LMS.Notification.Application.History.Ports;
using LMS.Notification.Domain.History;
using Microsoft.Extensions.Options;

namespace LMS.Notification.Application.History.Handlers;

// 通知记录没有物业归属，可见范围（Q01/Q11）尚未确定，不能默认全员工可读：目前只有全物业调用者可以读取，
// 限定在单一物业的调用者得到 403。投递来源未接入时返回“来源不可用”，不冒充空结果。
internal sealed class NotificationHistoryQueryHandler(
    INotificationHistoryReader reader,
    IOptions<NotificationHistoryOptions> options) : INotificationHistoryQueryHandler
{
    public async Task<OperationResult<PageResult<NotificationHistoryEntry>>> ListAsync(
        ListNotificationHistoryQuery query,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var rejection = Check<PageResult<NotificationHistoryEntry>>(scope);
        if (rejection is not null)
        {
            return rejection;
        }

        if (query.From is not null && query.To is not null && query.From >= query.To)
        {
            return OperationResult<PageResult<NotificationHistoryEntry>>.Invalid([new FieldIssue("to", "Must be later than from")]);
        }

        return OperationResult<PageResult<NotificationHistoryEntry>>.Succeeded(await reader.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<NotificationHistoryEntry>> GetAsync(
        string notificationId,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var rejection = Check<NotificationHistoryEntry>(scope);
        if (rejection is not null)
        {
            return rejection;
        }

        var entry = await reader.FindAsync(notificationId, cancellationToken);

        return entry is null
            ? OperationResult<NotificationHistoryEntry>.Failed(OperationOutcome.NotFound)
            : OperationResult<NotificationHistoryEntry>.Succeeded(entry);
    }

    private OperationResult<T>? Check<T>(PropertyScope scope)
    {
        if (!options.Value.DeliverySourceConnected)
        {
            return OperationResult<T>.Failed(OperationOutcome.SourceUnavailable);
        }

        return scope.IsAllProperties ? null : OperationResult<T>.Failed(OperationOutcome.Forbidden);
    }
}
