namespace LMS.Dispatch.Application.Directory;

// Booking 提供的待分配行程摘要，用于创建邀请。IsAssignable 为 false 表示行程当前不可分配（例如已指派或已取消）。
public sealed record AssignableTrip(
    string TripId,
    string TripReference,
    string PropertyId,
    bool IsAssignable,
    DateTimeOffset PickupAt,
    string PickupName,
    string DropoffName,
    int Passengers,
    string VehicleClass);
