using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Domain.Sources;

// 报表的服务类型：跨境路线或跨境接送产品记为 CROSS_BORDER，其余都是物业接送 PROPERTY_TRANSFER。
public static class ServiceTypes
{
    public const string CrossBorderProduct = "CROSS_BORDER_TRANSFER";

    public static ServiceType Of(bool isCrossBorderRoute, string? product) =>
        isCrossBorderRoute || product == CrossBorderProduct ? ServiceType.CrossBorder : ServiceType.PropertyTransfer;
}
