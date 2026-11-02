namespace LMS.BuildingBlocks.Identity.Authorization;

// 授权数据源不可用（例如下游身份服务无响应）。接入层映射为 503，而不是误判为无权限。
public sealed class CallerAuthorizationUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
