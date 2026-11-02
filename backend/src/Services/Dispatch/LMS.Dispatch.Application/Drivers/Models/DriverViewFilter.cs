namespace LMS.Dispatch.Application.Drivers.Models;

// 司机目录页签。对外字面值为 ALL / AVAILABLE / ON_TRIP / BLACKLIST（停用与暂停的司机）；与 propertyId、status、search 取交集。
public enum DriverViewFilter
{
    All,
    Available,
    OnTrip,
    Blacklist
}
