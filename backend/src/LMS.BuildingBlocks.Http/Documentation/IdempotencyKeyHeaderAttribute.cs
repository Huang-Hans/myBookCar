namespace LMS.BuildingBlocks.Http.Documentation;

/// <summary>
/// 声明动作读取必填的 Idempotency-Key 请求头，供 API 文档显示。
/// 请求头由 <c>RequestHeaders.ReadIdempotencyKey</c> 从 HttpContext 读取，没有形参，ApiExplorer 无法推断，因此需要显式声明。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class IdempotencyKeyHeaderAttribute : Attribute;
