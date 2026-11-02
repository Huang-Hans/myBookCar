using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.SelfService.Models;

// 客户自助预订的前置上下文：宾客、Platform 的自助预约规则，以及当前不能预订的原因（可预订时为 null）。
public sealed record GuestBookingContext(GuestProfile Guest, SelfBookingRules Rules, BookingBlock? Block);
