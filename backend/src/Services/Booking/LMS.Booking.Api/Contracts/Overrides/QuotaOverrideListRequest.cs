using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Overrides.Models;

namespace LMS.Booking.Api.Contracts.Overrides;

public sealed class QuotaOverrideListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(QuotaOverrideSorting.Fields, QuotaOverrideSorting.DefaultSort, QuotaOverrideSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
