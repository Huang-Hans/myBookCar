using LMS.Booking.Domain.Customers;

namespace LMS.Booking.Application.Customers.Models;

// 一个收藏项及其当前可用性。IsAvailable 为 null 表示这次无法读取目录，不能判断；false 表示地点已停用或已不存在。
public sealed record FavoriteLocationItem(LocationFavorite Favorite, bool? IsAvailable);
