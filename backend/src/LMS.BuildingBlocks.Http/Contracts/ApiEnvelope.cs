namespace LMS.BuildingBlocks.Http.Contracts;

/// <summary>
/// 成功响应体：所有成功的业务响应都是这个形状。
/// 失败时使用 <see cref="ApiErrorEnvelope"/>。
/// </summary>
/// <param name="Code">五位应用码：20000 通用成功，20100 新资源已创建，20200 异步任务已受理。</param>
/// <param name="Message">可展示的英文消息，成功固定为 success。</param>
/// <param name="Data">业务结果；无正文或为空时省略该键。</param>
/// <param name="Meta">仅在存在真实元数据时出现，例如列表分页。</param>
/// <param name="TraceId">W3C trace id，与响应头 X-Trace-Id 一致。</param>
public sealed record ApiEnvelope<T>(int Code, string Message, T? Data, ApiMeta? Meta, string TraceId);
