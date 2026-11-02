using LMS.BuildingBlocks.Core.Querying;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Models;

// 司机本人的检查记录列表。DriverId 来自令牌；From / To 按提交时间（From 含，To 不含）。
public sealed record ListInspectionsQuery(
    ListQuery List,
    string DriverId,
    InspectionPhase? Phase,
    bool? HasFailure,
    DateTimeOffset? From,
    DateTimeOffset? To);
