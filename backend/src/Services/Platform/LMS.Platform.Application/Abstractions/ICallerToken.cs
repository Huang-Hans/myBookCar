namespace LMS.Platform.Application.Abstractions;

// 当前请求调用者的 Bearer 令牌，仅用于向其他服务转发（例如读取 Booking 的每日限额）。
public interface ICallerToken
{
    string? Value { get; }
}
