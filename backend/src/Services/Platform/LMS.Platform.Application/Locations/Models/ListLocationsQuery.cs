using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Models;

// CategoryCode 按分类目录代码精确筛选；LocationIds 把结果限定在给定的地点集合内（用于解析收藏 / 最近地点的名称与状态，同样服务端分页）。
public sealed record ListLocationsQuery(
    ListQuery List,
    bool? IsActive,
    LocationRegion? Region,
    string? CategoryCode = null,
    IReadOnlyList<string>? LocationIds = null);
