using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Coupons.Models;
using LMS.Dispatch.Domain.Coupons;

namespace LMS.Dispatch.Api.Contracts.Coupons;

public sealed class CouponListRequest : PagedQueryRequest
{
    [Description("按綁定的會員編號篩選；傳入時不能為空白。")]
    public string? MemberId { get; set; }

    [Description("按類型篩選，取 FREE_TRIP / DISCOUNT / FARE_DEDUCTION。")]
    public CouponType? Type { get; set; }

    [Description("按狀態篩選，取 ACTIVE / UPCOMING / EXHAUSTED / DISABLED / EXPIRED。")]
    public CouponStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() =>
        new(CouponSorting.Fields, CouponSorting.DefaultSort, CouponSorting.StableField);
}
