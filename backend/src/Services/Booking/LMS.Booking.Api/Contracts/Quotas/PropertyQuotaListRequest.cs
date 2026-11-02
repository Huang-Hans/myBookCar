using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Quotas.Models;

namespace LMS.Booking.Api.Contracts.Quotas;

public sealed class PropertyQuotaListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(PropertyQuotaSorting.Fields, PropertyQuotaSorting.DefaultSort, PropertyQuotaSorting.StableField);

    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
