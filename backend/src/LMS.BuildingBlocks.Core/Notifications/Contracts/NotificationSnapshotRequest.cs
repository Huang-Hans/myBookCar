namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// getNotificationSnapshot 的请求。page 默认 1（>=1），pageSize 默认 20（1～100）；省略字段取默认值。
public sealed record NotificationSnapshotRequest(int Page = 1, int PageSize = 20)
{
    public const int MaxPageSize = 100;

    public bool IsValid => Page >= 1 && PageSize is >= 1 and <= MaxPageSize;
}
