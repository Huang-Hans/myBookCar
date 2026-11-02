using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Application.Reports.Models;

// 读取某一类报表的行。统计窗口取 [From, To)（两者都可不传，不传表示该侧不限）：只返回自身统计窗口完全落在该区间内的行（行是按窗口聚合的，
// 不能把跨出区间的窗口按比例拆分；具体口径待 Q04 确认）。PropertyId 为空表示 Scope 内的全部物业。
public sealed record ReportRowsQuery(
    ReportType Type,
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    ServiceType? ServiceType);
