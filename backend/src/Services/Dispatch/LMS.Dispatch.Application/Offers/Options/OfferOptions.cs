namespace LMS.Dispatch.Application.Offers.Options;

// 派单邀请设置（Offers 节）。邀请有效期由配置给定：原型的 30 秒倒计时只是交互参考，正式时长待业务确认（Q03）。
public sealed class OfferOptions
{
    public const string SectionName = "Offers";

    public const int MinimumValiditySeconds = 5;

    public int ValiditySeconds { get; init; } = 30;

    // 后台把已过期的待响应邀请写成 EXPIRED 的扫描间隔与单次处理上限；这是运维参数，不影响业务有效期。
    public int ExpirySweepSeconds { get; init; } = 10;

    public int ExpiryBatchSize { get; init; } = 100;

    // 配置值低于下限时按下限处理，避免邀请一创建就过期。
    public TimeSpan Validity => TimeSpan.FromSeconds(Math.Max(MinimumValiditySeconds, ValiditySeconds));
}
