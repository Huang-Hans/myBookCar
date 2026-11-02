namespace LMS.Booking.Application.SelfService.Models;

// 评估后确定的接车时间（ASAP 已按固定缓冲换算为具体时间）。
public sealed record ResolvedPickup(DateTimeOffset PickupAt);
