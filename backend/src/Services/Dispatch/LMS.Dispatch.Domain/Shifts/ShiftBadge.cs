namespace LMS.Dispatch.Domain.Shifts;

// 班次卡片上的徽章。已派车辆不单独保存，由是否写入车牌推导。
public enum ShiftBadge
{
    Preferred,
    Standby,
    OnDuty,
    Rest,
    Leave,
    OnTime,
    VehicleAssigned
}
