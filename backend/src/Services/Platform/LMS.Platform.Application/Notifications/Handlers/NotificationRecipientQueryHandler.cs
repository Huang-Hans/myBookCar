using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Authorization.Models;
using LMS.Platform.Application.Authorization.Services;
using LMS.Platform.Application.Notifications.Models;
using LMS.Platform.Application.Notifications.Options;
using LMS.Platform.Application.Users.Ports;
using LMS.Platform.Domain.Users;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Application.Notifications.Handlers;

// 解析某物业内有资格接收客户下单通知的客服（含离线客服），仅供 Booking 后台通过受限服务身份调用。
// 资格：已启用、已关联企业身份、角色在配置的允许集合内、物业范围包含订单物业，
// 且同时拥有 notifications:read、dispatch:read、bookings:read（权限与范围的最终判定与调用者授权解析完全一致）。
internal sealed class NotificationRecipientQueryHandler(
    IPortalUserRepository users,
    CallerAccessFactory accessFactory,
    IOptions<NotificationRecipientOptions> options) : INotificationRecipientQueryHandler
{
    private static readonly string[] RequiredPermissions =
        [PermissionCodes.NotificationsRead, PermissionCodes.DispatchRead, PermissionCodes.BookingsRead];

    public async Task<OperationResult<IReadOnlyList<string>>> HandleAsync(NotificationRecipientQuery query, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.Roles.Length == 0)
        {
            return OperationResult<IReadOnlyList<string>>.Failed(OperationOutcome.SourceUnavailable);
        }

        var candidates = await users.ListNotificationCandidatesAsync(query.PropertyId, settings.Roles, settings.MaxRecipients + 1, cancellationToken);
        if (candidates.Count > settings.MaxRecipients)
        {
            return OperationResult<IReadOnlyList<string>>.Failed(OperationOutcome.SourceUnavailable);
        }

        return OperationResult<IReadOnlyList<string>>.Succeeded(await ResolveRecipientsAsync(candidates, cancellationToken));
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(IReadOnlyList<PortalUser> candidates, CancellationToken cancellationToken)
    {
        var accesses = new List<CallerAccess?>();
        foreach (var candidate in candidates)
        {
            accesses.Add(await accessFactory.CreateAsync(candidate, cancellationToken));
        }

        return accesses
            .Where(access => access is not null && RequiredPermissions.All(access.Permissions.Contains))
            .Select(access => access!.UserId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
