using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Services;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Api.Mapping.Shifts;

internal static class ShiftBatchMapper
{
    public static RequestParseResult<ShiftBatchRules> ToRules(ShiftBatchRequest request)
    {
        var errors = new FieldErrorCollector();
        var rules = ReadRules(request, errors);
        if (!errors.HasErrors) errors.AddRange(ShiftBatchRuleValidation.Validate(rules));
        return errors.HasErrors ? RequestParseResult<ShiftBatchRules>.Fail(errors.ToFailure())
            : RequestParseResult<ShiftBatchRules>.Success(rules);
    }

    public static RequestParseResult<CreateShiftBatchCommand> ToCommand(CreateShiftBatchRequest request,
        string key, CommandContext context)
    {
        var rules = ToRules(request);
        if (rules.Failure is not null) return RequestParseResult<CreateShiftBatchCommand>.Fail(rules.Failure);
        var errors = new FieldErrorCollector();
        var hash = errors.RequiredSha256("previewHash", request.PreviewHash);
        return errors.HasErrors ? RequestParseResult<CreateShiftBatchCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateShiftBatchCommand>.Success(new(rules.Value, hash, key, context));
    }

    private static ShiftBatchRules ReadRules(ShiftBatchRequest request, FieldErrorCollector errors) => new(
        request.PropertyId is null ? null : errors.RequiredText("propertyId", request.PropertyId), ReadDriverIds(request.DriverIds, errors),
        errors.RequiredDate("from", request.From), errors.RequiredDate("to", request.To), request.Weekdays ?? [],
        errors.RequiredEnum<ShiftPeriod>("period", request.Period),
        errors.RequiredEnum<ShiftDutyStatus>("status", request.Status),
        errors.RequiredEnum<ShiftRole>("role", request.Role));

    private static IReadOnlyList<string> ReadDriverIds(IReadOnlyList<string?>? ids, FieldErrorCollector errors)
    {
        if (ids is null || ids.Count > ShiftBatchRuleValidation.MaxItems)
        {
            errors.Add("driverIds", "Select between 1 and 1000 drivers");
            return [];
        }
        return ids.Select((id, index) => errors.RequiredText($"driverIds[{index}]", id)).ToList();
    }

    public static ShiftBatchPreviewView ToView(ShiftBatchPreview preview) => new(preview.PreviewHash,
        preview.DriverCount, preview.DateCount, preview.CreateCount, preview.ExistingCount,
        preview.CreateCount > 0, preview.Items.Select(ToView).ToList());

    public static ShiftBatchView ToView(ShiftBatch batch) => new(batch.BatchId,
        batch.CreatedAt.UtcDateTime.ToString("O", CultureInfo.InvariantCulture), batch.CreatedCount,
        batch.SkippedCount, batch.Items.Select(ToView).ToList());

    private static ShiftBatchItemView ToView(ShiftBatchItem item) => new(
        item.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), item.DriverId, item.DriverName,
        item.IsExisting, item.Period is { } period ? WireEnum.ToWire(period) : null,
        item.DutyStatus is { } status ? WireEnum.ToWire(status) : null,
        item.Role is { } role ? WireEnum.ToWire(role) : null, item.ShiftId, item.PropertyId);
}
