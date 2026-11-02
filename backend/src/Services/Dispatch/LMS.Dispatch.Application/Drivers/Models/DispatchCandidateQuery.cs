using LMS.Dispatch.Domain.Vehicles;
using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Dispatch.Application.Drivers.Models;

// 派单候选司机查询：同物业、启用、AVAILABLE，并排除此刻已被邀请占用的司机；搜索匹配姓名与车辆文本。
public sealed record DispatchCandidateQuery(ListQuery List, string PropertyId, IReadOnlyList<string> ExcludedDriverIds, VehicleGrade? VehicleGrade = null)
{
    public IReadOnlyList<string>? ScheduledDriverIds { get; init; }
}
