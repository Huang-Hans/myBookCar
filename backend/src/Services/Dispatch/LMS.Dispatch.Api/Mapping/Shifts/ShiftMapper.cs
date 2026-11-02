using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.Http.Requests;
using LMS.Dispatch.Api.Contracts.Shifts;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Shifts.Commands;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Api.Mapping.Shifts;

internal static class ShiftMapper
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static RequestParseResult<ShiftBoardQuery> ToBoardQuery(ShiftBoardRequest request, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var pickupAt = errors.OptionalUtcInstant("pickupAt", request.PickupAt);
        if (request.Date is not null && request.PickupAt is not null)
            errors.Add("pickupAt", "Must not be combined with date");
        return errors.HasErrors
            ? RequestParseResult<ShiftBoardQuery>.Fail(errors.ToFailure())
            : RequestParseResult<ShiftBoardQuery>.Success(new(request.Date, request.PropertyId, context.Scope, pickupAt));
    }

    public static ShiftBoardQuery ToUpcomingQuery(ShiftUpcomingRequest request, CommandContext context) =>
        new(null, request.PropertyId, context.Scope);

    public static RequestParseResult<ShiftCalendarQuery> ToCalendarQuery(ShiftCalendarRequest request, CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var query = new ShiftCalendarQuery(
            errors.RequiredDate("from", request.From),
            errors.RequiredDate("to", request.To),
            request.PropertyId,
            context.Scope);

        return errors.HasErrors
            ? RequestParseResult<ShiftCalendarQuery>.Fail(errors.ToFailure())
            : RequestParseResult<ShiftCalendarQuery>.Success(query);
    }

    public static RequestParseResult<CreateShiftCommand> ToCreateCommand(
        CreateShiftRequest request,
        string idempotencyKey,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var command = new CreateShiftCommand(
            errors.RequiredDate("businessDate", request.BusinessDate),
            errors.RequiredEnum<ShiftPeriod>("period", request.Period),
            errors.RequiredText("driverId", request.DriverId),
            ReadStatus(errors, request.Status),
            ReadRole(errors, request.Role),
            idempotencyKey,
            context);

        return errors.HasErrors
            ? RequestParseResult<CreateShiftCommand>.Fail(errors.ToFailure())
            : RequestParseResult<CreateShiftCommand>.Success(command);
    }

    public static RequestParseResult<UpdateShiftCommand> ToUpdateCommand(
        string shiftId,
        long expectedVersion,
        PatchShiftRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var changes = new ShiftChanges(
            errors.PatchEnum<ShiftDutyStatus>("status", request.Status),
            errors.PatchEnum<ShiftRole>("role", request.Role),
            errors.PatchEnum<ShiftAttendance>("attendance", request.Attendance));
        if (changes.ChangedFields().Count == 0)
        {
            errors.Add(string.Empty, "At least one field is required");
        }

        return errors.HasErrors
            ? RequestParseResult<UpdateShiftCommand>.Fail(errors.ToFailure())
            : RequestParseResult<UpdateShiftCommand>.Success(new UpdateShiftCommand(shiftId, expectedVersion, changes, context));
    }

    public static RequestParseResult<AssignShiftVehicleCommand> ToAssignCommand(
        string shiftId,
        long expectedVersion,
        AssignShiftVehicleRequest request,
        CommandContext context)
    {
        var errors = new FieldErrorCollector();
        var plate = errors.RequiredText("vehiclePlate", request.VehiclePlate).Trim();

        return errors.HasErrors
            ? RequestParseResult<AssignShiftVehicleCommand>.Fail(errors.ToFailure())
            : RequestParseResult<AssignShiftVehicleCommand>.Success(new AssignShiftVehicleCommand(shiftId, expectedVersion, plate, context));
    }

    public static ShiftBoardView ToView(ShiftBoard board) =>
        new(
            board.TimeZoneId,
            FormatDate(board.BusinessDate),
            board.PropertyId,
            board.Periods.Select(ToView).ToList());

    public static ShiftUpcomingView ToView(ShiftUpcoming upcoming) =>
        new(
            upcoming.TimeZoneId,
            FormatDate(upcoming.From),
            FormatDate(upcoming.To),
            upcoming.PropertyId,
            upcoming.Days.Select(day => new ShiftUpcomingDayView(FormatDate(day.BusinessDate), day.Shifts.Count, day.Shifts.Select(ToView).ToList())).ToList());

    public static ShiftView ToView(ShiftCard card) => ToView(card.Shift, card.DriverName, card.Badges);

    private static ShiftColumnView ToView(ShiftColumn column) =>
        new(
            WireEnum.ToWire(column.Period),
            FormatTime(column.Window.StartsAt),
            FormatTime(column.Window.EndsAt),
            column.Window.EndsNextDay,
            column.Shifts.Count,
            column.Shifts.Select(ToView).ToList());

    private static ShiftView ToView(DriverShift shift, string? driverName, IReadOnlyList<ShiftBadge> badges)
    {
        var window = ShiftWindows.Of(shift.Period);

        return new ShiftView(
            shift.ShiftId,
            FormatDate(shift.BusinessDate),
            WireEnum.ToWire(shift.Period),
            FormatTime(window.StartsAt),
            FormatTime(window.EndsAt),
            window.EndsNextDay,
            shift.DriverId,
            driverName,
            shift.PropertyId,
            shift.VehiclePlate,
            WireEnum.ToWire(shift.DutyStatus),
            WireEnum.ToWire(shift.Role),
            WireEnum.ToWire(shift.Attendance),
            badges.Select(WireEnum.ToWire).ToList(),
            shift.Version,
            shift.VehicleGrade is { } grade ? WireEnum.ToWire(grade) : null);
    }

    // 省略状态时按「当值」。省略角色时按首选，与看板上大多数卡片一致。
    private static ShiftDutyStatus ReadStatus(FieldErrorCollector errors, string? status) =>
        status is null ? ShiftDutyStatus.OnDuty : errors.RequiredEnum<ShiftDutyStatus>("status", status);

    private static ShiftRole ReadRole(FieldErrorCollector errors, string? role) =>
        role is null ? ShiftRole.Preferred : errors.RequiredEnum<ShiftRole>("role", role);

    private static string FormatDate(DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string FormatTime(TimeOnly time) => time.ToString(TimeFormat, CultureInfo.InvariantCulture);
}
