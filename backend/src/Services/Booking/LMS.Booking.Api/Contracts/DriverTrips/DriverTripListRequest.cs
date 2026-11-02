using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Api.Contracts.DriverTrips;

public sealed class DriverTripListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(DriverTripSorting.Fields, DriverTripSorting.DefaultSort, DriverTripSorting.StableField);

    [Description("ACTIVE 为当前有效（已指派未终态）的行程；HISTORY 为已完成或已取消且归属本人的行程。不传时为 ACTIVE。")]
    public DriverTripView View { get; set; } = DriverTripView.Active;

    [Description("仅用于 HISTORY：按终态筛选，取 COMPLETED 或 CANCELLED；不传时为全部终态。")]
    public TripStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
