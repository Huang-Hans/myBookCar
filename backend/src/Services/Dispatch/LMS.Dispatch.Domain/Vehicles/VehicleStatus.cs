namespace LMS.Dispatch.Domain.Vehicles;

// 车辆状态。对应车辆管理顶部四张卡片：可用、行程中、维修中、停用。
public enum VehicleStatus
{
    Available,
    OnTrip,
    Maintenance,
    OutOfService
}
