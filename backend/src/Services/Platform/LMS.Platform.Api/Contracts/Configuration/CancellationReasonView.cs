namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 取消原因配置视图，提供原因代码及其关联的积分退还政策引用。
/// </summary>
/// <param name="ReasonCode">取消原因代码。</param>
/// <param name="Name">原因显示名称。</param>
/// <param name="PointsReturnPolicyCode">关联的积分退还政策代码。</param>
/// <param name="PointsReturnReference">外部积分退还政策说明。</param>
/// <param name="Source">外部参考来源标签。</param>
public sealed record CancellationReasonView(
    string ReasonCode,
    string Name,
    string PointsReturnPolicyCode,
    string PointsReturnReference,
    string Source);
