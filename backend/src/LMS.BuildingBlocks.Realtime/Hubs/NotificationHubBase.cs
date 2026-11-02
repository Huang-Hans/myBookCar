using Microsoft.AspNetCore.SignalR;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 三个通知 Hub 的共同基类。Hub 只做两件事：连接时由服务端解析接收身份并加入个人组，断开时清理。
// 不提供下单、派单、接单或任意加入组的方法；具体 Hub 只可额外声明只读方法。
public abstract class NotificationHubBase(NotificationConnectionRegistry registry) : Hub
{
    // 解析本次连接的接收身份；返回 null 表示拒绝（未关联、已停用、权限不足）。
    protected abstract Task<NotificationConnectionAccess?> AuthorizeConnectionAsync(CancellationToken cancellationToken);

    protected NotificationTrackedConnection? CurrentConnection => registry.Find(Context.ConnectionId);

    protected NotificationConnectionAccess? CurrentAccess => CurrentConnection?.Access;

    public override async Task OnConnectedAsync()
    {
        var access = await TryAuthorizeAsync();
        if (access is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, access.Group);
        registry.Add(Context, access);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        registry.Remove(Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }

    private async Task<NotificationConnectionAccess?> TryAuthorizeAsync()
    {
        try
        {
            return await AuthorizeConnectionAsync(Context.ConnectionAborted);
        }
        catch (NotificationVerificationUnavailableException)
        {
            return null;
        }
    }
}
