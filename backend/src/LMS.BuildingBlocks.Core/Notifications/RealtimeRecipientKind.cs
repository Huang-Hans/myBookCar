namespace LMS.BuildingBlocks.Core.Notifications;

// 实时通知的接收端类型。同一个字符串标识在不同类型下属于不同接收范围。
public enum RealtimeRecipientKind
{
    Staff,
    Driver,
    Customer
}
