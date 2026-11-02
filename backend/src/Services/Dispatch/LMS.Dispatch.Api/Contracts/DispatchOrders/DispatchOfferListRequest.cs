using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Offers.Models;

namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

public sealed class DispatchOfferListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(DispatchOfferSorting.Fields, DispatchOfferSorting.DefaultSort, DispatchOfferSorting.StableField);

    public override ListQuerySpec GetSortSpecification() => Spec;
}
