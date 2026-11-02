namespace LMS.Booking.Api.Contracts.DriverTrips;

/// <summary>
/// 待分配行程摘要（服务间，供 Dispatch 创建邀请）。isAssignable 为 false 表示行程当前不可分配。
/// </summary>
public sealed record AssignmentCandidateView(
    string TripId,
    string TripReference,
    string PropertyId,
    bool IsAssignable,
    DateTimeOffset PickupAt,
    string PickupName,
    string DropoffName,
    int Passengers,
    string VehicleClass);

/// <summary>
/// 司机当前有效行程的引用（服务间）。
/// </summary>
public sealed record TripReferenceView(string TripId, string TripReference);
