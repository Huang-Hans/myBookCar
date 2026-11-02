namespace LMS.Platform.Application.Locations.Models;

// 排序请求中的一项：地点标识和提交者读到的版本。
public sealed record LocationOrderItem(string LocationId, long ExpectedVersion);
