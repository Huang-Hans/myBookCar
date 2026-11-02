namespace LMS.Booking.Application.SelfService.Ports;

// 客户可见的地点信息：不含联系人、联系电话、围栏等员工管理字段。
public sealed record CatalogLocationDetail(string LocationId, string Name, string Address, string Region, string CategoryCode, string OperatingHours);
