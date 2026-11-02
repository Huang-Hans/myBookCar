using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Application.Coupons.Models;

public sealed record ListCouponsQuery(
    ListQuery List,
    PropertyScope Scope,
    DateOnly Today,
    string? MemberId,
    CouponType? Type,
    CouponStatus? Status);
