using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Vehicles.Models;
using LMS.Dispatch.Domain.Vehicles;

namespace LMS.Dispatch.Api.Contracts.Vehicles;

public sealed class VehicleListRequest : PagedQueryRequest
{
    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("按等级筛选，取 BUSINESS_SEDAN / LUXURY_MPV / LUXURY_SEDAN。")]
    public VehicleGrade? Grade { get; set; }

    [Description("按状态筛选，取 AVAILABLE / ON_TRIP / MAINTENANCE / OUT_OF_SERVICE。")]
    public VehicleStatus? Status { get; set; }

    public override ListQuerySpec GetSortSpecification() =>
        new(VehicleSorting.Fields, VehicleSorting.DefaultSort, VehicleSorting.StableField);
}
