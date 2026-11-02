using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Models;

public sealed record CouponCard(Coupon Coupon, DateOnly Today);
