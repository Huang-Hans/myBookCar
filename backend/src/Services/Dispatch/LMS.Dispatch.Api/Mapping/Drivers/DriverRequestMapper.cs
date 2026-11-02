using LMS.BuildingBlocks.Core.Security;
using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Drivers;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Api.Mapping.Drivers;

internal static class DriverRequestMapper
{
    public static ListDriversQuery ToListQuery(DriverListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.Status, request.View);

    public static RequestParseResult<CreateDriverCommand> ToCreateCommand(
        CreateDriverRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateDriverCommand(
            errors.RequiredText("name", request.Name),
            errors.RequiredText("licenseNumber", request.LicenseNumber),
            errors.RequiredDate("licenseExpiryDate", request.LicenseExpiryDate),
            ReadPhone(errors, request.Phone),
            errors.RequiredText("propertyId", request.PropertyId),
            string.IsNullOrWhiteSpace(request.AssignedVehicleId) ? null : request.AssignedVehicleId.Trim(),
            errors.RequiredEnum<DriverStatus>("status", request.Status),
            ReadLanguages(errors, request.Languages),
            ReadEmergencyContact(errors, request.EmergencyContact),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateDriverCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateDriverCommand>.Success(command);
    }

    public static RequestParseResult<UpdateDriverCommand> ToUpdateCommand(
        string driverId,
        long expectedVersion,
        PatchDriverRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new DriverChanges(
            errors.PatchText("name", request.Name),
            errors.PatchText("licenseNumber", request.LicenseNumber),
            errors.PatchDate("licenseExpiryDate", request.LicenseExpiryDate),
            PatchPhone(errors, request.Phone),
            errors.PatchText("propertyId", request.PropertyId),
            request.AssignedVehicleId.PatchNullableText(),
            errors.PatchEnum<DriverStatus>("status", request.Status),
            PatchLanguages(errors, request.Languages),
            ReadEmergencyContactPatch(errors, request.EmergencyContact));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateDriverCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateDriverCommand>.Success(new UpdateDriverCommand(driverId, expectedVersion, changes, context));
    }

    // 司机联系电话同时是手机号密码登录的标识：保存前按统一规范规范化（含国家/地区码的完整号码），登录查询才能与库内 phone 精确相等。
    private static string ReadPhone(FieldErrorCollector errors, string? value)
    {
        var text = errors.RequiredText("phone", value);

        return text.Length == 0 ? text : NormalizeOrReport(errors, text);
    }

    private static Optional<string> PatchPhone(FieldErrorCollector errors, Optional<string?> value)
    {
        var text = errors.PatchText("phone", value);

        return text.IsSet ? Optional<string>.Of(NormalizeOrReport(errors, text.Value!)) : text;
    }

    private static string NormalizeOrReport(FieldErrorCollector errors, string text)
    {
        if (PhoneNumber.TryNormalize(text, out var normalized))
        {
            return normalized;
        }

        errors.Add("phone", "Must be a phone number, for example +85361234567 or 13800138001");

        return text;
    }

    private static IReadOnlyList<string> ReadLanguages(FieldErrorCollector errors, List<string?>? languages)
    {
        if (languages is null)
        {
            errors.Add("languages", "Is required");
            return [];
        }

        return languages.Select((language, index) => errors.RequiredText($"languages[{index}]", language)).ToList();
    }

    private static Optional<IReadOnlyList<string>> PatchLanguages(FieldErrorCollector errors, Optional<List<string?>?> languages)
    {
        if (!languages.IsSet)
        {
            return default;
        }

        return Optional<IReadOnlyList<string>>.Of(ReadLanguages(errors, languages.Value));
    }

    private static EmergencyContact ReadEmergencyContact(FieldErrorCollector errors, EmergencyContactRequest? value)
    {
        if (value is null)
        {
            errors.Add("emergencyContact", "Is required");
            return new EmergencyContact(string.Empty, string.Empty);
        }

        return new EmergencyContact(
            errors.RequiredText("emergencyContact.name", value.Name),
            errors.RequiredText("emergencyContact.phone", value.Phone));
    }

    private static EmergencyContactChanges? ReadEmergencyContactPatch(
        FieldErrorCollector errors,
        Optional<EmergencyContactPatchRequest?> value)
    {
        if (!value.IsSet)
        {
            return null;
        }

        if (value.Value is null)
        {
            errors.Add("emergencyContact", "Must not be null");
            return null;
        }

        return new EmergencyContactChanges(
            errors.PatchText("emergencyContact.name", value.Value.Name),
            errors.PatchText("emergencyContact.phone", value.Value.Phone));
    }
}
