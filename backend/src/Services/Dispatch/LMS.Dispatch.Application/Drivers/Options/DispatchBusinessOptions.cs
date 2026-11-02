namespace LMS.Dispatch.Application.Drivers.Options;

// 业务日期所用的时区。停用生效日等“业务日期”按它计算，默认与合同中的原型时区一致。
public sealed class DispatchBusinessOptions
{
    public const string SectionName = "Business";

    public string TimeZoneId { get; init; } = "Asia/Macau";
}
