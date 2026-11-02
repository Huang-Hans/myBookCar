namespace LMS.Booking.Application.SelfService.Models;

// 一个服务城市及其覆盖的地点区域（区域字面值与 Platform 地点区域一致）。
public sealed record CityOffering(ServiceCity City, IReadOnlyList<string> Regions);
