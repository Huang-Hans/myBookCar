namespace LMS.Reporting.Application.Idempotency.Models;

// 幂等作用域：服务、操作、经认证的主体和调用方提供的键。不同主体的相同键不会互相命中。
public sealed record IdempotencyScope(string ServiceName, string Operation, string Subject, string Key);
