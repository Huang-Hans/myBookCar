namespace LMS.Booking.Api.Contracts.Previews;

/// <summary>
/// 所选车型的积分预览。LMS 只记录积分引用，不结算：available 是会员快照里的值，estimatedBalanceAfter 是预计值，不是已扣减的实际余额。
/// </summary>
/// <param name="Required">所选车型需要的积分（来自 Console:VehicleClasses 配置）。</param>
/// <param name="Available">会员快照中的积分；没有可信值时省略。</param>
/// <param name="Shortfall">积分差额（不足部分，足够为 0）；available 未知时省略。</param>
/// <param name="EstimatedBalanceAfter">按 available 减 required 计算的预计余额；available 未知时省略。</param>
/// <param name="Basis">口径说明，恒为 REFERENCE_ESTIMATE。</param>
public sealed record BookingPreviewPointsView(long Required, long? Available, long? Shortfall, long? EstimatedBalanceAfter, string Basis);
