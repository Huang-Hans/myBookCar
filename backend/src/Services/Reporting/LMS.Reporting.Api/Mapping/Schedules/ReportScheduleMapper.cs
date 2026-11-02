using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Reporting.Api.Contracts.Schedules;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Schedules.Commands;
using LMS.Reporting.Application.Schedules.Models;
using LMS.Reporting.Domain.Rows;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Api.Mapping.Schedules;

internal static class ReportScheduleMapper
{
    private const string RegisteredStatus = "REGISTERED";

    public static ListReportSchedulesQuery ToListQuery(ReportScheduleListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope);

    public static RequestParseResult<CreateReportScheduleCommand> ToCreateCommand(
        CreateReportScheduleRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateReportScheduleCommand(
            errors.RequiredEmail("recipientEmail", request.RecipientEmail),
            ReadFrequency(errors, request.Frequency),
            errors.RequiredEnum<ReportType>("reportType", request.ReportType),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateReportScheduleCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateReportScheduleCommand>.Success(command);
    }

    public static ReportScheduleView ToView(ReportSchedule schedule) => new(
        schedule.ScheduleId,
        schedule.RecipientEmail,
        schedule.Frequency,
        WireEnum.ToWire(schedule.ReportType),
        schedule.Version,
        RegisteredStatus);

    private static string ReadFrequency(FieldErrorCollector errors, string? value)
    {
        var frequency = errors.RequiredText("frequency", value);
        if (frequency.Length > 0 && !ScheduleFrequency.IsValid(frequency))
        {
            errors.Add("frequency", $"Must be one of: {string.Join(", ", ScheduleFrequency.All)}");
        }

        return frequency;
    }
}
