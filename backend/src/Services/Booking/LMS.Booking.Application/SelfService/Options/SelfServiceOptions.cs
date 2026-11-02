namespace LMS.Booking.Application.SelfService.Options;

// 客户自助预订设置（SelfService 节）。物业侧的预订规则（提前量、最远预订天数、每日活跃订单数）与资格等级来自 Platform 的
// 自助预约规则 / 自助资格说明（CF15/CF16），这里只放 Platform 没有给出、需要部署确认的项（业务决定 Q02/Q05/Q06）。
// 列表属性可空 + Effective* 回退：配置绑定对非空集合是追加语义，可空绑定属性整体来自配置，未配置时走内置默认。
public sealed class SelfServiceOptions
{
    public const string SectionName = "SelfService";

    // 是否允许立即出发（ASAP）。Platform 自助规则的最短提前量通常大于 0，与原型的 Now 选项冲突，默认关闭，待业务确认。
    public bool AsapEnabled { get; init; }

    // 客户自助取消使用的取消原因代码，必须存在于 Platform 取消原因清单（Q06）。
    public string CancellationReasonCode { get; init; } = "CR-003";

    // 距接车时间不足该分钟数时不再允许客户自助取消；0 表示不设窗口，只遵守状态机的取消限制（Q06 基线）。
    public int CancellationCutoffMinutes { get; init; }

    public IReadOnlyList<ServiceProductOption>? Products { get; set; }

    public IReadOnlyList<ServiceProductOption> EffectiveProducts => Products ?? DefaultProducts;

    private static IReadOnlyList<ServiceProductOption> DefaultProducts { get; } =
    [
        new("BUSINESS_SEDAN", "Standard Sedan", "Standard sedan transfer within the same city.",
            true, "ONE_WAY", "LOCAL", null, "BUSINESS_SEDAN"),
        new("LIMOUSINE", "Limousine", "Private limousine transfer between locations within the same city.",
            true, "ONE_WAY", "LOCAL", null, "LUXURY_SEDAN"),
        new("CROSS_BORDER_TRANSFER", "Cross-Border Transfer", "Transfer between Macau and Hong Kong.",
            true, "ONE_WAY", "CROSS_BORDER", null, "LUXURY_MPV"),
        // Party 已按业务要求开放预订（原 Q05 的容量模型待定项不再阻塞）；车型与容量仍复用 Console:VehicleClasses 的三档。
        new("PARTY", "Party", "Group transfer for parties.",
            true, "ONE_WAY", "LOCAL", null, "LUXURY_MPV")
    ];
}
