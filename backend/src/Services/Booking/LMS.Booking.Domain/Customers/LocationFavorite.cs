namespace LMS.Booking.Domain.Customers;

// 收藏的一个目录地点：地点标识与保存时的名称、区域快照。地点之后更名或停用时，收藏仍保留原引用与快照，读取时另行给出可用性。
public sealed record LocationFavorite(string LocationId, string Name, string Region);
