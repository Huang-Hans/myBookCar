namespace LMS.Booking.Domain.Trips;

// 车型。容量与积分价来自部署配置（Console:VehicleClasses，实施基线 D4），枚举只定义合法编码。
public enum VehicleClass
{
    LuxurySedan,
    LuxuryMpv,
    Maybach,
    BusinessSedan
}
