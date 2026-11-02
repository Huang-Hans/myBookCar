using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Commands;

public sealed record UpdateCouponCommand(string CouponId, long ExpectedVersion, CouponChanges Changes, CommandContext Context);
