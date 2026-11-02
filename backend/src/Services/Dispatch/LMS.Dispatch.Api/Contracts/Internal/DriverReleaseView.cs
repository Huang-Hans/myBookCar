namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 司机释放结果：处理后的可用状态。
/// </summary>
/// <param name="DriverId">司机标识。</param>
/// <param name="Availability">处理后的可用状态；没有同步值时省略。</param>
public sealed record DriverReleaseView(string DriverId, string? Availability);
