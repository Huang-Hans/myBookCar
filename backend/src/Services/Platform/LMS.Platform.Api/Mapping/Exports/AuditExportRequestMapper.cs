using LMS.BuildingBlocks.Http.Requests;
using LMS.Platform.Api.Contracts.Exports;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Exports.Commands;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Api.Mapping.Exports;

internal static class AuditExportRequestMapper
{
    public static RequestParseResult<CreateAuditExportCommand> ToCommand(
        CreateAuditExportRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var filter = ReadFilter(errors, request.Filter);

        return errors.HasErrors
            ? RequestParseResult<CreateAuditExportCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateAuditExportCommand>.Success(new CreateAuditExportCommand(filter, idempotencyKey, context));
    }

    private static AuditExportFilter ReadFilter(FieldErrorCollector errors, AuditExportFilterRequest? value)
    {
        if (value is null)
        {
            errors.Add("filter", "Is required");
            return new AuditExportFilter(null, null, null, null);
        }

        return new AuditExportFilter(
            OptionalText(errors, "filter.propertyId", value.PropertyId),
            errors.OptionalUtcInstant("filter.from", value.From),
            errors.OptionalUtcInstant("filter.to", value.To),
            string.IsNullOrWhiteSpace(value.Search) ? null : value.Search.Trim());
    }

    // 传入的物业代码不能是空白；没传表示当前有权的全部范围。
    private static string? OptionalText(FieldErrorCollector errors, string field, string? value)
    {
        if (value is null)
        {
            return null;
        }

        return errors.RequiredText(field, value);
    }
}
