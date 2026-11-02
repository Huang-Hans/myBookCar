using LMS.BuildingBlocks.Core.Notifications;

namespace LMS.BuildingBlocks.Realtime.Hubs;

// 连接建立时由服务端解析出的接收身份：接收端类型与本地实体标识（员工为 Platform 本地 userId，不是 JWT sub；
// 司机为 driverId，客户为 guestId）。组名由服务器决定，客户端不能传入收件人、物业组或订阅范围。
// AccessToken 仅用于员工连接在有效期内重新核验授权，只保存在进程内存，不落库、不记日志。
public sealed record NotificationConnectionAccess(RealtimeRecipientKind Kind, string RecipientId, string? AccessToken = null)
{
    public string Group => NotificationHubGroups.For(Kind, RecipientId);
}
