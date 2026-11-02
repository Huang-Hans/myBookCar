using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.ReportingSources.Models;

// 报表来源的一条班次，附带司机当前姓名；司机主档已不存在时姓名为 null。
public sealed record ReportingShift(DriverShift Shift, string? DriverName);
