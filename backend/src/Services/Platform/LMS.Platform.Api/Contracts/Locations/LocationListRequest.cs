using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Api.Contracts.Locations;

public sealed class LocationListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(LocationSorting.Fields, LocationSorting.DefaultSort, LocationSorting.StableField);

    [Description("按启用状态筛选；不传时不过滤。")]
    public bool? IsActive { get; set; }

    [Description("按区域筛选，取值为区域枚举的对外字面值，例如 MACAU_PENINSULA。")]
    public LocationRegion? Region { get; set; }

    [Description("按分类目录代码筛选（如 LC-001）；与 region 是不同维度，互不替代。传入时不能为空白。")]
    public string? CategoryCode { get; set; }

    [MaxLength(2000, ErrorMessage = "Must not exceed 2000 characters")]
    [Description("逗号分隔的地点标识，结果限定在这些地点内（最多取前 100 个），用于解析收藏与最近地点的名称和启用状态。")]
    public string? LocationIds { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
