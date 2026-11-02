using LMS.Booking.Domain.TripLocations;

namespace LMS.Booking.Application.TripLocations.Commands;

// 司机上报一批定位点；字段范围已在接入层校验，DriverId 来自令牌绑定。
public sealed record RecordTripLocationsCommand(string TripId, string DriverId, IReadOnlyList<LocationPoint> Points);
