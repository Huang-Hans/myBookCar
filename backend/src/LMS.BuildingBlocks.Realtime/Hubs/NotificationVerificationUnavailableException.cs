namespace LMS.BuildingBlocks.Realtime.Hubs;

// 授权数据源暂时不可用（例如 Platform 无响应）。连接建立时按拒绝处理；已建立的连接不因此被撤销，下个周期再核验。
public sealed class NotificationVerificationUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
