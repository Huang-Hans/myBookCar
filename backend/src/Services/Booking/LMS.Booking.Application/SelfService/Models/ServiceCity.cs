namespace LMS.Booking.Application.SelfService.Models;

// 服务城市。地点所属区域决定城市：HONG_KONG 区域为香港，其余（澳门半岛、路氹、机场）为澳门，与跨境判定口径一致。
public enum ServiceCity
{
    Macau,
    HongKong
}
