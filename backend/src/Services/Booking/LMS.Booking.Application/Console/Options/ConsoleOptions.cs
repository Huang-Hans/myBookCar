namespace LMS.Booking.Application.Console.Options;

// 客服控制台一期的车型/权益配置（实施基线 D4）：车型容量与积分价为部署配置，等级→车型权益同样由配置给定。
// 列表属性可空 + Effective* 回退：配置绑定对非空集合是"追加"语义，直接给默认值会把 appsettings 条目
// 重复叠进去触发启动校验失败；可空绑定属性整体来自配置，未配置时走内置默认。
public sealed class ConsoleOptions
{
    public const string SectionName = "Console";

    // 预约单最短提前分钟数；ASAP 单不受此限制（服务端按固定缓冲落库）。
    public int MinimumLeadTimeMinutes { get; init; } = 24 * 60;

    // 站内通知保留天数；0 表示不启用 TTL 清理。
    public int NotificationRetentionDays { get; init; } = 90;

    public IReadOnlyList<VehicleClassOption>? VehicleClasses { get; set; }

    public IReadOnlyList<TierEntitlementOption>? TierEntitlements { get; set; }

    public IReadOnlyList<VehicleClassOption> EffectiveVehicleClasses => VehicleClasses ?? DefaultVehicleClasses;

    public IReadOnlyList<TierEntitlementOption> EffectiveTierEntitlements => TierEntitlements ?? DefaultTierEntitlements;

    private static IReadOnlyList<VehicleClassOption> DefaultVehicleClasses { get; } =
    [
        new("BUSINESS_SEDAN", 3, 2, 3000),
        new("LUXURY_SEDAN", 3, 2, 3000),
        new("LUXURY_MPV", 6, 4, 4500)
    ];

    private static IReadOnlyList<TierEntitlementOption> DefaultTierEntitlements { get; } =
    [
        new("SILVER", ["BUSINESS_SEDAN", "LUXURY_SEDAN"]),
        new("GOLD", ["BUSINESS_SEDAN", "LUXURY_SEDAN", "LUXURY_MPV"]),
        new("PLATINUM", ["BUSINESS_SEDAN", "LUXURY_SEDAN", "LUXURY_MPV"]),
        new("DIAMOND", ["BUSINESS_SEDAN", "LUXURY_SEDAN", "LUXURY_MPV"]),
        new("VIP", ["BUSINESS_SEDAN", "LUXURY_SEDAN", "LUXURY_MPV"])
    ];
}

// 一个车型：编码（域枚举的线上值）、乘客/行李容量与所需积分（仅引用展示，不结算）。
public sealed record VehicleClassOption(string Code, int MaxPassengers, int MaxLuggage, long PointsRequired);

// 一个等级可用的车型集合；空列表表示该等级不具备参与资格。
public sealed record TierEntitlementOption(string TierCode, IReadOnlyList<string> VehicleClasses);
