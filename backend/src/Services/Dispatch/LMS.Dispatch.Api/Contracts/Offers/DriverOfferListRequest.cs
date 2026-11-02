using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Offers.Models;

namespace LMS.Dispatch.Api.Contracts.Offers;

public sealed class DriverOfferListRequest : PagedQueryRequest
{
    public override ListQuerySpec GetSortSpecification() =>
        new(OfferSorting.Fields, OfferSorting.DefaultSort, OfferSorting.StableField);
}
