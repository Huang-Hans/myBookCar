using System.Globalization;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Domain.Shifts;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Api.Mapping.Shifts;

internal static class ShiftWithdrawalMapper
{
    public static RequestParseResult<WithdrawShiftsCommand> ToCommand(WithdrawShiftsRequest request, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var date = errors.RequiredDate("businessDate", request.BusinessDate);
        var period = errors.RequiredEnum<ShiftPeriod>("period", request.Period);
        var property = request.PropertyId is null ? null : errors.RequiredText("propertyId", request.PropertyId);
        var wholePeriod = errors.RequiredBool("isWholePeriod", request.IsWholePeriod);
        var targets = ReadTargets(request.Targets, errors);
        ValidateSelection(wholePeriod, targets, errors);
        return errors.HasErrors ? RequestParseResult<WithdrawShiftsCommand>.Fail(errors.ToFailure())
            : RequestParseResult<WithdrawShiftsCommand>.Success(new(date, period, property, wholePeriod, targets, context));
    }

    private static void ValidateSelection(bool wholePeriod, IReadOnlyList<ShiftWithdrawalTarget> targets, FieldErrorCollector errors)
    {
        ValidateCount(wholePeriod, targets.Count, errors);
        if (targets.Select(target => target.ShiftId).Distinct(StringComparer.Ordinal).Count() != targets.Count)
            errors.Add("targets", "Shift IDs must be unique");
    }

    private static void ValidateCount(bool wholePeriod, int count, FieldErrorCollector errors)
    {
        if (!wholePeriod && count != 1) errors.Add("targets", "Select exactly one shift");
    }

    private static IReadOnlyList<ShiftWithdrawalTarget> ReadTargets(IReadOnlyList<ShiftWithdrawalTargetRequest?>? targets,
        FieldErrorCollector errors)
    {
        if (targets is null || targets.Count is < 1 or > 1000)
        {
            errors.Add("targets", "Select between 1 and 1000 shifts");
            return [];
        }
        return targets.Select((target, index) => ReadTarget(target, index, errors)).ToList();
    }

    private static ShiftWithdrawalTarget ReadTarget(ShiftWithdrawalTargetRequest? target, int index, FieldErrorCollector errors)
    {
        var id = errors.RequiredText($"targets[{index}].shiftId", target?.ShiftId);
        if (target?.Version is not > 0) errors.Add($"targets[{index}].version", "A positive shift version is required");
        return new(id, target?.Version ?? 0);
    }

    public static ShiftWithdrawalView ToView(ShiftWithdrawalResult result) =>
        new(result.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), WireEnum.ToWire(result.Period), result.WithdrawnCount);
}
