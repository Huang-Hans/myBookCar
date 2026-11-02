using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Reports;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Reports.Commands;
using LMS.Dispatch.Application.Reports.Models;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Api.Mapping.Reports;

internal static class ReportMapper
{
    public static VehicleIssueView ToView(VehicleIssue issue) => new(
        issue.IssueId, issue.PropertyId, issue.VehicleSnapshot, issue.Description, issue.InspectionId,
        WireEnum.ToWire(issue.Status), issue.ReportedAt, issue.Version);

    public static DriverIncidentView ToView(DriverIncident incident) => new(
        incident.IncidentId, incident.PropertyId, incident.VehicleSnapshot, WireEnum.ToWire(incident.Type), incident.OccurredAt,
        incident.Description, incident.TripId, WireEnum.ToWire(incident.Status), incident.ReportedAt, incident.Version);

    public static OperationsContactsView ToView(OperationsContacts contacts) => new(
        contacts.PropertyId,
        contacts.IsAvailable,
        contacts.Contacts.Select(contact => new OperationsContactItemView(contact.Name, contact.Channel, contact.Value)).ToList());

    public static ListVehicleIssuesQuery ToListQuery(VehicleIssueListRequest request, string driverId) =>
        new(request.ToListQuery(), driverId, request.Status);

    public static ListIncidentsQuery ToListQuery(DriverIncidentListRequest request, string driverId) =>
        new(request.ToListQuery(), driverId, request.Type, request.Status);

    public static RequestParseResult<CreateVehicleIssueCommand> ToIssueCommand(
        string driverId,
        CreateVehicleIssueRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateVehicleIssueCommand(
            driverId,
            LimitedText(errors, "description", request.Description, VehicleIssue.MaxDescriptionLength),
            OptionalText(errors, "inspectionId", request.InspectionId),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateVehicleIssueCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateVehicleIssueCommand>.Success(command);
    }

    public static RequestParseResult<CreateIncidentCommand> ToIncidentCommand(
        string driverId,
        CreateIncidentRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateIncidentCommand(
            driverId,
            errors.RequiredEnum<IncidentType>("type", request.Type),
            RequiredInstant(errors, request.OccurredAt),
            LimitedText(errors, "description", request.Description, DriverIncident.MaxDescriptionLength),
            OptionalText(errors, "tripId", request.TripId),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateIncidentCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateIncidentCommand>.Success(command);
    }

    private static string LimitedText(FieldErrorCollector errors, string field, string? value, int maxLength)
    {
        var text = errors.RequiredText(field, value);
        if (text.Length > maxLength)
        {
            errors.Add(field, $"Must not exceed {maxLength} characters");
        }

        return text;
    }

    private static string? OptionalText(FieldErrorCollector errors, string field, string? value) =>
        value is null ? null : errors.RequiredText(field, value);

    private static DateTimeOffset RequiredInstant(FieldErrorCollector errors, DateTimeOffset? value)
    {
        if (value is null)
        {
            errors.Add("occurredAt", "Is required");
            return default;
        }

        return value.Value;
    }
}
