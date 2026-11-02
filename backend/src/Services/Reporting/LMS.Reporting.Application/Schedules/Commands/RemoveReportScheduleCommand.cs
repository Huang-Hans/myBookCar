using LMS.Reporting.Application.Abstractions;

namespace LMS.Reporting.Application.Schedules.Commands;

public sealed record RemoveReportScheduleCommand(string ScheduleId, CommandContext Context);
