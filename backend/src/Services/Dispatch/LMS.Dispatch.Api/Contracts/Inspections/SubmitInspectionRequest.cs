namespace LMS.Dispatch.Api.Contracts.Inspections;

/// <summary>单项检查结果。</summary>
/// <param name="ItemCode">检查项代码，来自模板。</param>
/// <param name="Result">PASS 或 FAIL；FAIL 也是合法结果。</param>
public sealed record InspectionAnswerRequest(string? ItemCode, string? Result);

/// <summary>
/// 提交检查的请求体。需要 Idempotency-Key 头；司机、物业、车辆与提交时间由服务端取得，不接受请求自报。
/// </summary>
/// <param name="Phase">PRE_TRIP 或 POST_TRIP。</param>
/// <param name="TemplateVersion">所基于的模板版本；省略表示当前发布版本。</param>
/// <param name="Answers">逐项结果：模板的每一项恰好出现一次，不允许缺项、重复或未知项。</param>
public sealed record SubmitInspectionRequest(string? Phase, int? TemplateVersion, IReadOnlyList<InspectionAnswerRequest>? Answers);
