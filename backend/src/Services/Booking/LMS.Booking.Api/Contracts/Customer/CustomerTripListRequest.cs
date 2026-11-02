using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Trips.Models;

namespace LMS.Booking.Api.Contracts.Customer;

public sealed class CustomerTripListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(GuestTripSorting.Fields, GuestTripSorting.DefaultSort, GuestTripSorting.StableField);

    [Description("订单分组：ACTIVE（全部非终态）/ COMPLETED / CANCELLED。不传时为 ACTIVE。")]
    public GuestTripGroup Group { get; set; } = GuestTripGroup.Active;

    public override ListQuerySpec GetSortSpecification() => Spec;
}
