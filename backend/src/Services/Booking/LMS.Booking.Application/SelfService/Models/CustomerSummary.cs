using LMS.Booking.Domain.Guests;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.SelfService.Models;

// 客户本人摘要：宾客资料、预订能力（Block 为 null 即可预订）与首页当前旅程（没有时为 null）。
public sealed record CustomerSummary(GuestProfile Guest, BookingBlock? Block, Trip? CurrentTrip);
