using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Reporting.Application.Schedules.Models;

// 只列出对调用者当前授权范围可见的计划。
public sealed record ListReportSchedulesQuery(ListQuery List, PropertyScope Scope);
