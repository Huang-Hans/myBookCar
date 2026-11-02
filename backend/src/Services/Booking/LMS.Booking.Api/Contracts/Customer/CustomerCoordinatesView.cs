namespace LMS.Booking.Api.Contracts.Customer;

/// <summary>
/// 地图坐标，单位为度。数值原样来自 Platform 地点目录 coordinates，不做坐标系转换；目录的坐标参考系尚未确认（Q02），客户端不应假设特定坐标系的偏移修正。
/// </summary>
/// <param name="Latitude">纬度，-90～90。</param>
/// <param name="Longitude">经度，-180～180。</param>
public sealed record CustomerCoordinatesView(double Latitude, double Longitude);
