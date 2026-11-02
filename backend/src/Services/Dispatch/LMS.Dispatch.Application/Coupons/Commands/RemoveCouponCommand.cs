using LMS.Dispatch.Application.Abstractions;

namespace LMS.Dispatch.Application.Coupons.Commands;

public sealed record RemoveCouponCommand(string CouponId, CommandContext Context);
