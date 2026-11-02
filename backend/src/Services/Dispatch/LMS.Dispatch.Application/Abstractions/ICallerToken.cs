namespace LMS.Dispatch.Application.Abstractions;

// 当前请求调用者的 Bearer 令牌，仅用于向 Platform 转发以解析调用者授权和核对物业代码。
public interface ICallerToken
{
    string? Value { get; }
}
