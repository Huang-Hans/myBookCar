using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Core.Patching;
using LMS.Platform.Api.Contracts.Configuration;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Commands;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Api.Mapping.Configuration;

internal static class PropertyRequestMapper
{
    public static RequestParseResult<CreatePropertyCommand> Create(CreatePropertyRequest request, string key, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var property = new ConfiguredProperty(
            errors.RequiredText("propertyId", request.PropertyId).ToUpperInvariant(),
            Text(request.Name),
            Text(request.Address),
            Text(request.ContactPerson),
            Text(request.ContactPhone),
            errors.RequiredInt("assignedFleetCount", request.AssignedFleetCount),
            Text(request.OperatingHours),
            Text(request.ServiceLevel),
            errors.RequiredInt("minimumLeadTimeHours", request.MinimumLeadTimeHours),
            Text(request.CancellationPolicyReference),
            Text(request.MembershipEligibility),
            Text(request.VipOverrideRule),
            request.IsActive ?? true);
        if (string.IsNullOrWhiteSpace(property.Name))
            errors.Add("name", "Is required and must not be blank");
        return errors.HasErrors
            ? RequestParseResult<CreatePropertyCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreatePropertyCommand>.Success(new(property, key, context));
    }

    public static RequestParseResult<UpdatePropertyCommand> Patch(string id, long version, PatchPropertyRequest request, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = ReadChanges(errors, request);
        if (changes.ChangedFields().Count == 0)
            errors.Add(string.Empty, "At least one field is required");
        return errors.HasErrors
            ? RequestParseResult<UpdatePropertyCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdatePropertyCommand>.Success(new(id, version, changes, context));
    }

    private static PropertyChanges ReadChanges(FieldErrorCollector errors, PatchPropertyRequest request) => new(
        errors.PatchText("name", request.Name),
        ReadText(errors, "address", request.Address),
        ReadText(errors, "contactPerson", request.ContactPerson),
        ReadText(errors, "contactPhone", request.ContactPhone),
        errors.PatchInt("assignedFleetCount", request.AssignedFleetCount),
        ReadText(errors, "operatingHours", request.OperatingHours),
        ReadText(errors, "serviceLevel", request.ServiceLevel),
        errors.PatchInt("minimumLeadTimeHours", request.MinimumLeadTimeHours),
        ReadText(errors, "cancellationPolicyReference", request.CancellationPolicyReference),
        ReadText(errors, "membershipEligibility", request.MembershipEligibility),
        ReadText(errors, "vipOverrideRule", request.VipOverrideRule),
        errors.PatchBool("isActive", request.IsActive));

    private static string Text(string? value) => value?.Trim() ?? string.Empty;

    private static Optional<string> ReadText(
        FieldErrorCollector errors, string field, Optional<string?> value)
    {
        if (!value.IsSet) return default;
        if (value.Value is null) errors.Add(field, "Must not be null");
        return Optional<string>.Of(Text(value.Value));
    }
}
