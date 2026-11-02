using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Shifts.Models;
using LMS.Dispatch.Application.Shifts.Ports;
using LMS.Dispatch.Domain.Shifts;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class PickupShiftQuery(BusinessCalendar calendar, IDriverShiftPeriodReader shifts, IDriverShiftRepository repository)
{
    public ShiftSlot Resolve(DateTimeOffset pickupAt)
    {
        var local = calendar.LocalTimeAt(pickupAt);
        var date = DateOnly.FromDateTime(local);
        var time = TimeOnly.FromDateTime(local);
        var morning = ShiftWindows.Of(ShiftPeriod.Morning);
        if (time < morning.StartsAt) return new(date.AddDays(-1), ShiftPeriod.Night);
        return new(date, PeriodAt(time));
    }

    public Task<IReadOnlyList<DriverShift>> ListAsync(ShiftSlot slot, PropertyScope scope,
        string? propertyId, CancellationToken cancellationToken) =>
        shifts.ListOnDutyAsync(slot.BusinessDate, slot.Period, scope, propertyId, cancellationToken);

    public async Task<bool> IsOnDutyAsync(string driverId, DateTimeOffset pickupAt,
        string propertyId, CancellationToken cancellationToken)
    {
        var slot = Resolve(pickupAt);
        var shift = await repository.FindByDriverInPeriodAsync(driverId, slot.BusinessDate, slot.Period, cancellationToken);
        return shift is { DutyStatus: ShiftDutyStatus.OnDuty } && shift.PropertyId == propertyId;
    }

    private static ShiftPeriod PeriodAt(TimeOnly time)
    {
        if (time < ShiftWindows.Of(ShiftPeriod.Morning).EndsAt) return ShiftPeriod.Morning;
        return time < ShiftWindows.Of(ShiftPeriod.Afternoon).EndsAt ? ShiftPeriod.Afternoon : ShiftPeriod.Night;
    }
}
