using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed class DriverShiftListRequest : PagedQueryRequest
{
    public override ListQuerySpec GetSortSpecification() =>
        new(DriverShiftSorting.Fields, DriverShiftSorting.DefaultSort, DriverShiftSorting.StableField);
}
