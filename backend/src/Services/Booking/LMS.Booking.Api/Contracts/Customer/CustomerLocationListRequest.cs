using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;

namespace LMS.Booking.Api.Contracts.Customer;

public sealed class CustomerLocationListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(["name"], "name", "name");

    [Description("按地点区域筛选：MACAU_PENINSULA / COTAI / HONG_KONG / AIRPORT；传入时不能为空白。")]
    public string? Region { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
