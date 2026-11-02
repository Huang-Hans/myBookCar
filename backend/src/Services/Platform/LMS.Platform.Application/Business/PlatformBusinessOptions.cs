namespace LMS.Platform.Application.Business;

// 业务日期所用的时区。客户限制的有效期（含边界日）按它计算，默认与合同中的原型时区一致。
public sealed class PlatformBusinessOptions
{
    public const string SectionName = "Business";

    public string TimeZoneId { get; init; } = "Asia/Macau";
}
