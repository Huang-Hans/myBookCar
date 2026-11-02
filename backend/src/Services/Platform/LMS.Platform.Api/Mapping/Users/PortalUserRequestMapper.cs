using LMS.BuildingBlocks.Core.Patching;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.Users;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Users.Commands;
using LMS.Platform.Application.Users.Models;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Api.Mapping.Users;

internal static class PortalUserRequestMapper
{
    public static ListPortalUsersQuery ToListQuery(PortalUserListRequest request, PropertyScope scope) =>
        new(request.ToListQuery(), scope, request.PropertyId, request.IsActive);

    public static RequestParseResult<CreatePortalUserCommand> ToCreateCommand(
        CreatePortalUserRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreatePortalUserCommand(
            errors.RequiredText("name", request.Name),
            errors.RequiredText("corporateEmail", request.CorporateEmail),
            errors.RequiredEnum<PortalRole>("roleCode", request.RoleCode),
            errors.RequiredBool("hasAllPropertyAccess", request.HasAllPropertyAccess),
            RequiredNullableProperty(errors, request.PropertyId),
            errors.RequiredBool("isActive", request.IsActive),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreatePortalUserCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreatePortalUserCommand>.Success(command);
    }

    public static RequestParseResult<UpdatePortalUserCommand> ToUpdateCommand(
        string userId,
        long expectedVersion,
        PatchPortalUserRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new PortalUserChanges(
            errors.PatchText("name", request.Name),
            errors.PatchText("corporateEmail", request.CorporateEmail),
            errors.PatchEnum<PortalRole>("roleCode", request.RoleCode),
            errors.PatchBool("hasAllPropertyAccess", request.HasAllPropertyAccess),
            PatchNullableProperty(errors, request.PropertyId),
            errors.PatchBool("isActive", request.IsActive));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdatePortalUserCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdatePortalUserCommand>.Success(new UpdatePortalUserCommand(userId, expectedVersion, changes, context));
    }

    // 必填但可空：键必须出现；null 表示全物业授权，非 null 时不得为空白。
    private static string? RequiredNullableProperty(FieldErrorCollector errors, Optional<string?> value)
    {
        if (!value.IsSet)
        {
            errors.Add("propertyId", "Is required; use null for all-property access");
            return null;
        }

        return NormalizeProperty(errors, value.Value);
    }

    private static Optional<string> PatchNullableProperty(FieldErrorCollector errors, Optional<string?> value) =>
        value.IsSet ? Optional<string>.Of(NormalizeProperty(errors, value.Value)) : default;

    private static string? NormalizeProperty(FieldErrorCollector errors, string? value)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("propertyId", "Must be null or a non-blank property code");
            return null;
        }

        return value.Trim();
    }
}
