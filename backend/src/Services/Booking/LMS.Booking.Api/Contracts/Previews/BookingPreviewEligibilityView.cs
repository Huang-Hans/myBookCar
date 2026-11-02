namespace LMS.Booking.Api.Contracts.Previews;

/// <summary>资格摘要，来自会员快照与部署配置的等级权益。</summary>
/// <param name="Tier">宾客等级代码。</param>
/// <param name="TierMinimum">最低可参与等级；配置中没有任何等级具备资格时省略。</param>
/// <param name="PlayVerified">游玩核验是否通过。</param>
/// <param name="PlayWindowDays">游玩核验窗口天数。</param>
/// <param name="EntitledVehicleClasses">该等级可用的车型。</param>
/// <param name="IsVehicleEntitled">所选车型是否在可用车型内。</param>
public sealed record BookingPreviewEligibilityView(
    string Tier,
    string? TierMinimum,
    bool PlayVerified,
    int PlayWindowDays,
    IReadOnlyList<string> EntitledVehicleClasses,
    bool IsVehicleEntitled);
