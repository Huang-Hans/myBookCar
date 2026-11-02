using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Api.Contracts.Drivers;

public sealed class DriverListRequest : PagedQueryRequest
{
    [Description("按物业筛选；传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("按司机状态筛选。")]
    public DriverStatus? Status { get; set; }

    [Description("页面标签筛选，与其他条件取交集；黑名单视图包含停用与暂停的司机。")]
    public DriverViewFilter View { get; set; } = DriverViewFilter.All;

    public override ListQuerySpec GetSortSpecification() =>
        new(DriverSorting.Fields, DriverSorting.DefaultSort, DriverSorting.StableField);
}
