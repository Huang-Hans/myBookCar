namespace LMS.BuildingBlocks.Core.Notifications.Contracts;

// 服务端调用客户端方法 notificationReceived 的参数：沿用统一 Envelope 的 code / message / data / traceId。
// code 固定 20000，message 固定 success；traceId 是产生该事件的业务请求的 traceId。
public sealed record NotificationMessageV1(int Code, string Message, NotificationEventDataV1 Data, string TraceId)
{
    public const int SuccessCode = 20000;
    public const string SuccessMessage = "success";
    public const int CurrentSchemaVersion = 1;
}
