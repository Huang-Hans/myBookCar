using System.Globalization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Dispatch.Api.Contracts.ReportingSources;
using LMS.Dispatch.Application.ReportingSources.Models;

namespace LMS.Dispatch.Api.Mapping.ReportingSources;

internal static class ReportingShiftMapper
{
    private const string DateFormat = "yyyy-MM-dd";

    public static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static ReportingShiftView ToView(ReportingShift item)
    {
        var shift = item.Shift;

        return new(
            shift.ShiftId,
            shift.BusinessDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            WireEnum.ToWire(shift.Period),
            shift.DriverId,
            item.DriverName,
            shift.PropertyId,
            WireEnum.ToWire(shift.DutyStatus));
    }
}
