namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 失败响应体：不含 data 与 meta，字段级原因放在 errors。
/// </summary>
/// <param name="Code">五位应用码，与 HTTP 状态相符，例如 40001 字段校验失败、40400 资源不存在。</param>
/// <param name="Message">可展示的英文消息，不含堆栈、SQL 或内部类名；errors 非空时取第一条字段错误的 message。</param>
/// <param name="Errors">字段级错误；总是存在，没有字段原因时为空数组。</param>
/// <param name="TraceId">W3C trace id，与响应头 X-Trace-Id 一致。</param>
public sealed record ApiErrorEnvelope(int Code, string Message, IReadOnlyList<ApiFieldError> Errors, string TraceId);
