using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Booking.Application.Trips.Models;

namespace LMS.Booking.Api.Contracts.CustomerRecords;

public sealed class ReviewableTripListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(GuestTripSorting.Fields, "-closedAt,-pickupAt,tripId", GuestTripSorting.StableField);

    public override ListQuerySpec GetSortSpecification() => Spec;
}
