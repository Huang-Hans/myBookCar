namespace LMS.Booking.Api.Contracts.TripLocations;

/// <summary>
/// 司机上报一批定位点。
/// </summary>
/// <param name="CoordinateSystem">坐标系，固定为 GCJ02；设备原始坐标须先转换。</param>
/// <param name="Points">1 至 50 个定位点。</param>
public sealed record TripLocationUploadRequest(string? CoordinateSystem, List<TripLocationPointRequest?>? Points);
