using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Drivers.Models;

namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

public sealed class DispatchCandidateListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(DispatchCandidateSorting.Fields, DispatchCandidateSorting.DefaultSort, DispatchCandidateSorting.StableField);

    public override ListQuerySpec GetSortSpecification() => Spec;
}
